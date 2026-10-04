#pragma warning disable OPENAI001

using DotNet8WebAPI.Services.ApplicationHealth;
using Microsoft.ApplicationInsights;
using OpenAI.Responses;
using Serilog.Context;
using System.Diagnostics;
using System.Text.Json;

namespace DotNet8WebAPI.Services.AI;

public class AiService : IAiService
{
    private const string Instructions =
        "You are an assistant for this .NET application. Use only the explicitly registered tools. " +
        "For application health questions, always call get_application_health. " +
        "For recent-error questions, call get_recent_application_errors. " +
        "When asked why the application is unhealthy, check health first and, if it is unhealthy or degraded, " +
        "then inspect recent errors. For requests asking about both health and recent errors, call both tools. " +
        "For questions about this repository's architecture, use search_application_knowledge and cite returned source paths. " +
        "For books, use get_all_books. Never invent health, error, book, or repository facts. " +
        "Clearly distinguish telemetry facts from your inferences. If a tool reports Unavailable, say so.";

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly IAiModelClient _modelClient;
    private readonly IAgentToolExecutor _toolExecutor;
    private readonly AiAgentOptions _options;
    private readonly ILogger<AiService> _logger;
    private readonly TelemetryClient _telemetryClient;

    public AiService(
        IAiModelClient modelClient,
        IAgentToolExecutor toolExecutor,
        Microsoft.Extensions.Options.IOptions<AiAgentOptions> options,
        ILogger<AiService> logger,
        TelemetryClient telemetryClient)
    {
        _modelClient = modelClient;
        _toolExecutor = toolExecutor;
        _options = options.Value;
        _logger = logger;
        _telemetryClient = telemetryClient;
    }

    public async Task<string> AskAsync(
        string message,
        CancellationToken cancellationToken = default,
        string? correlationId = null)
    {
        var maxMessageLength = Math.Clamp(_options.MaxMessageLength, 1, 20_000);
        if (string.IsNullOrWhiteSpace(message) || message.Length > maxMessageLength)
        {
            throw new ArgumentException(
                $"The message must contain 1 to {maxMessageLength} characters.",
                nameof(message));
        }

        correlationId = ResolveCorrelationId(correlationId);
        var maxIterations = Math.Clamp(_options.MaxToolIterations, 1, 12);
        var maxToolCalls = Math.Clamp(_options.MaxToolCalls, 1, 30);
        var timeoutSeconds = Math.Clamp(_options.TimeoutSeconds, 1, 300);
        var stopwatch = Stopwatch.StartNew();
        var toolCallCount = 0;
        var eventProperties = new Dictionary<string, string>
        {
            ["CorrelationId"] = correlationId,
            ["Provider"] = _modelClient.ProviderName,
            ["Model"] = _modelClient.ModelName
        };

        using var logScope = LogContext.PushProperty("CorrelationId", correlationId);
        TrackEvent("AI.Agent.Started", eventProperties);
        _logger.LogInformation(
            "AI agent request started. Provider: {Provider}, Model: {Model}, CorrelationId: {CorrelationId}",
            _modelClient.ProviderName,
            _modelClient.ModelName,
            correlationId);

        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        var inputItems = new List<ResponseItem>
        {
            ResponseItem.CreateDeveloperMessageItem(Instructions),
            ResponseItem.CreateUserMessageItem(message)
        };

        try
        {
            for (var iteration = 0; iteration < maxIterations; iteration++)
            {
                timeoutSource.Token.ThrowIfCancellationRequested();

                var response = await _modelClient.CreateResponseAsync(
                    inputItems,
                    _toolExecutor.Tools,
                    timeoutSource.Token);

                inputItems.AddRange(response.OutputItems);
                var functionCalls = response.OutputItems
                    .OfType<FunctionCallResponseItem>()
                    .ToArray();

                if (functionCalls.Length == 0)
                {
                    stopwatch.Stop();
                    TrackCompleted(correlationId, stopwatch.ElapsedMilliseconds, toolCallCount, "Completed");
                    _logger.LogInformation(
                        "AI agent request completed. Provider: {Provider}, ToolCallCount: {ToolCallCount}, DurationMs: {DurationMs}, CorrelationId: {CorrelationId}",
                        _modelClient.ProviderName,
                        toolCallCount,
                        stopwatch.ElapsedMilliseconds,
                        correlationId);
                    return response.OutputText;
                }

                foreach (var functionCall in functionCalls)
                {
                    toolCallCount++;
                    var toolProperties = new Dictionary<string, string>(eventProperties)
                    {
                        ["ToolName"] = functionCall.FunctionName,
                        ["ToolCallCount"] = toolCallCount.ToString()
                    };
                    TrackEvent("AI.Agent.ToolCalled", toolProperties);
                    var toolStopwatch = Stopwatch.StartNew();

                    string toolResult;
                    var toolFailed = false;
                    if (toolCallCount > maxToolCalls)
                    {
                        toolFailed = true;
                        toolResult = JsonSerializer.Serialize(
                            new { status = "LimitReached", message = "The request reached its tool-call limit." },
                            JsonOptions);
                    }
                    else
                    {
                        try
                        {
                            toolResult = await _toolExecutor.ExecuteAsync(
                                functionCall.FunctionName,
                                functionCall.FunctionArguments.ToString(),
                                functionCall.CallId,
                                timeoutSource.Token);
                        }
                        catch (OperationCanceledException) when (timeoutSource.IsCancellationRequested)
                        {
                            throw;
                        }
                        catch (Exception ex)
                        {
                            toolFailed = true;
                            _logger.LogError(
                                ex,
                                "Unexpected AI tool executor failure for {ToolName}, CorrelationId: {CorrelationId}",
                                functionCall.FunctionName,
                                correlationId);
                            toolResult = JsonSerializer.Serialize(
                                new { status = "Unavailable", message = "Tool execution failed." },
                                JsonOptions);
                        }

                    }

                    toolStopwatch.Stop();
                    toolProperties["DurationMs"] = toolStopwatch.ElapsedMilliseconds.ToString();
                    toolProperties["ResultStatus"] = ReadStatus(toolResult);
                    toolFailed |= toolProperties["ResultStatus"] is "Unavailable" or "LimitReached" or "Rejected";
                    TrackEvent(
                        toolFailed ? "AI.Agent.ToolFailed" : "AI.Agent.ToolSucceeded",
                        toolProperties);
                    _logger.LogInformation(
                        "AI tool completed. ToolName: {ToolName}, DurationMs: {DurationMs}, CorrelationId: {CorrelationId}",
                        functionCall.FunctionName,
                        toolStopwatch.ElapsedMilliseconds,
                        correlationId);

                    inputItems.Add(new FunctionCallOutputResponseItem(
                        functionCall.CallId,
                        toolResult));
                }
            }

            stopwatch.Stop();
            TrackCompleted(correlationId, stopwatch.ElapsedMilliseconds, toolCallCount, "IterationLimitReached");
            _logger.LogWarning(
                "AI agent reached its tool-iteration limit. ToolCallCount: {ToolCallCount}, CorrelationId: {CorrelationId}",
                toolCallCount,
                correlationId);
            return "I couldn't complete the request within the configured tool-use limit. Please try a more focused question.";
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            TrackFailed(correlationId, stopwatch.ElapsedMilliseconds, toolCallCount, "Timeout");
            _logger.LogWarning(
                "AI agent request timed out. CorrelationId: {CorrelationId}",
                correlationId);
            return "The AI request timed out before it could be completed. Please try again.";
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            TrackFailed(correlationId, stopwatch.ElapsedMilliseconds, toolCallCount, "Cancelled");
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            TrackFailed(correlationId, stopwatch.ElapsedMilliseconds, toolCallCount, "Failed");
            _logger.LogError(
                ex,
                "AI agent request failed. Provider: {Provider}, CorrelationId: {CorrelationId}",
                _modelClient.ProviderName,
                correlationId);
            throw;
        }
    }

    private static bool IsSafeCorrelationId(string? correlationId)
    {
        return !string.IsNullOrWhiteSpace(correlationId) &&
            correlationId.Length <= 128 &&
            correlationId.All(character =>
                char.IsAsciiLetterOrDigit(character) || character is '-' or '_' or '.');
    }

    private static string ResolveCorrelationId(string? correlationId)
    {
        return IsSafeCorrelationId(correlationId)
            ? correlationId!
            : Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");
    }

    private void TrackCompleted(
        string correlationId,
        long durationMs,
        int toolCallCount,
        string outcome)
    {
        TrackEvent("AI.Agent.Completed", new Dictionary<string, string>
        {
            ["CorrelationId"] = correlationId,
            ["Provider"] = _modelClient.ProviderName,
            ["Model"] = _modelClient.ModelName,
            ["DurationMs"] = durationMs.ToString(),
            ["ToolCallCount"] = toolCallCount.ToString(),
            ["Outcome"] = outcome
        });
    }

    private void TrackFailed(
        string correlationId,
        long durationMs,
        int toolCallCount,
        string reason)
    {
        TrackEvent("AI.Agent.Failed", new Dictionary<string, string>
        {
            ["CorrelationId"] = correlationId,
            ["Provider"] = _modelClient.ProviderName,
            ["Model"] = _modelClient.ModelName,
            ["DurationMs"] = durationMs.ToString(),
            ["ToolCallCount"] = toolCallCount.ToString(),
            ["Reason"] = reason
        });
    }

    private void TrackEvent(string name, Dictionary<string, string> properties)
    {
        _telemetryClient.TrackEvent(name, properties);
    }

    private static string ReadStatus(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind == JsonValueKind.Array)
            {
                return "Completed";
            }

            if (root.ValueKind != JsonValueKind.Object)
            {
                return "Unknown";
            }

            if (!root.TryGetProperty("status", out var status))
            {
                return "Completed";
            }

            return status.ValueKind == JsonValueKind.String
                ? status.GetString() ?? "Unknown"
                : "Unknown";
        }
        catch (JsonException)
        {
            return "Unknown";
        }
    }
}

#pragma warning restore OPENAI001