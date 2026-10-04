#pragma warning disable OPENAI001

using OpenAI.Responses;
using System.Text.Json;

namespace DotNet8WebAPI.Services.AI;

public sealed class AgentToolExecutor : IAgentToolExecutor
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IReadOnlyDictionary<string, AgentToolDefinition> _definitions;
    private readonly ILogger<AgentToolExecutor> _logger;

    public AgentToolExecutor(
        IEnumerable<AgentToolDefinition> definitions,
        ILogger<AgentToolExecutor> logger)
    {
        _definitions = definitions.ToDictionary(
            definition => definition.Name,
            StringComparer.Ordinal);
        _logger = logger;

        Tools = _definitions.Values
            .Select(definition => (ResponseTool)ResponseTool.CreateFunctionTool(
                definition.Name,
                definition.Parameters,
                strictModeEnabled: true,
                functionDescription: definition.Description))
            .ToArray();
    }

    public IReadOnlyList<ResponseTool> Tools { get; }

    public async Task<string> ExecuteAsync(
        string toolName,
        string argumentsJson,
        string callId,
        CancellationToken cancellationToken)
    {
        if (!_definitions.TryGetValue(toolName, out var definition))
        {
            _logger.LogWarning("Rejected unregistered AI tool request: {ToolName}", toolName);
            return JsonSerializer.Serialize(
                new AgentToolResult("Rejected", "This tool is not available."),
                JsonOptions);
        }

        if (definition.Policy == ToolExecutionPolicy.RequiresConfirmation)
        {
            _logger.LogInformation(
                "AI tool requires explicit approval: {ToolName}",
                toolName);
            return JsonSerializer.Serialize(
                new PendingToolApprovalResult
                {
                    ToolName = toolName,
                    ApprovalId = callId
                },
                JsonOptions);
        }

        try
        {
            using var document = JsonDocument.Parse(argumentsJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return JsonSerializer.Serialize(
                    new AgentToolResult("Rejected", "Tool arguments must be a JSON object."),
                    JsonOptions);
            }

            var result = await definition.ExecuteAsync(
                document.RootElement,
                cancellationToken);
            return JsonSerializer.Serialize(result, JsonOptions);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (JsonException)
        {
            _logger.LogWarning("Rejected invalid JSON arguments for AI tool {ToolName}", toolName);
            return JsonSerializer.Serialize(
                new AgentToolResult("Rejected", "Tool arguments were invalid."),
                JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI tool execution failed for {ToolName}", toolName);
            return JsonSerializer.Serialize(
                new AgentToolResult("Unavailable", "Tool execution failed. Details were recorded in application logs."),
                JsonOptions);
        }
    }

    private sealed record AgentToolResult(string Status, string Message);
}
