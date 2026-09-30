#pragma warning disable OPENAI001

using DotNet8WebAPI.Application.Books;
using DotNet8WebAPI.Model;
using DotNet8WebAPI.Services.ApplicationHealth;
using OpenAI.Responses;
using System.Diagnostics;
using System.Text.Json;

namespace DotNet8WebAPI.Services.AI;

public class AiService : IAiService
{
    private readonly ResponsesClient _client;
    private readonly IConfiguration _configuration;
    private readonly IBookService _bookService;
    private readonly IApplicationHealthService _applicationHealthService;
    private readonly ILogger<AiService> _logger;
    private readonly IHostEnvironment _environment;

    public AiService(
        IConfiguration configuration,
        IBookService bookService,
        IApplicationHealthService applicationHealthService,
        ILogger<AiService> logger,
        IHostEnvironment environment)
    {
        _configuration = configuration;
        _bookService = bookService;
        _applicationHealthService = applicationHealthService;
        _logger = logger;
        _environment = environment;

        var apiKey = _configuration["OpenAI:ApiKey"]
            ?? throw new InvalidOperationException(
                "OpenAI API key is not configured.");

        _client = new ResponsesClient(apiKey);
    }

    private static readonly FunctionTool GetAllBooksTool =
        ResponseTool.CreateFunctionTool(
            functionName: "get_all_books",
            functionDescription: "Gets all books from the database.",
            functionParameters: BinaryData.FromString("""
            {
                "type": "object",
                "properties": {},
                "additionalProperties": false
            }
            """),
            strictModeEnabled: true);

    private static readonly FunctionTool GetApplicationHealthTool =
        ResponseTool.CreateFunctionTool(
            functionName: "get_application_health",
            functionDescription:
                "Checks the current application health and database connectivity. " +
                "Use this tool for application health, status, availability, or recent problem questions. " +
                "The tool has no parameters.",
            functionParameters: BinaryData.FromString("""
            {
                "type": "object",
                "properties": {},
                "additionalProperties": false
            }
            """),
            strictModeEnabled: true);

    public async Task<string> AskAsync(string message)
    {
        var model = _configuration["OpenAI:Model"]
            ?? throw new InvalidOperationException(
                "OpenAI model is not configured.");

        var inputItems = new List<ResponseItem>
        {
            ResponseItem.CreateDeveloperMessageItem(
                "You are an assistant for a .NET application. " +
                "When the user asks about books, use the available book tools. " +
                "When the user asks about application health, status, availability, " +
                "or recent application problems, always use get_application_health. " +
                "Do not invent book or health data, and base health answers only on the tool result.")
        };

        inputItems.Add(
            ResponseItem.CreateUserMessageItem(message));

        while (true)
        {
            var options = new CreateResponseOptions(
                model,
                inputItems)
            {
                Tools =
                {
                    GetAllBooksTool,
                    GetApplicationHealthTool
                }
            };

            var response =
                await _client.CreateResponseAsync(options);

            inputItems.AddRange(response.Value.OutputItems);

            var toolWasCalled = false;

            foreach (var outputItem in response.Value.OutputItems)
            {
                if (outputItem is not FunctionCallResponseItem functionCall)
                {
                    continue;
                }

                switch (functionCall.FunctionName)
                {
                    case "get_all_books":
                        {
                            var books = await _bookService.GetAllBooks();

                            var toolResult =
                                JsonSerializer.Serialize(books);

                            inputItems.Add(
                                new FunctionCallOutputResponseItem(
                                    functionCall.CallId,
                                    toolResult));

                            toolWasCalled = true;
                            break;
                        }

                    case "get_application_health":
                        {
                            var stopwatch = Stopwatch.StartNew();
                            _logger.LogInformation(
                                "AI tool execution started: {ToolName}",
                                functionCall.FunctionName);

                            string toolResult;

                            try
                            {
                                var health =
                                    await _applicationHealthService.CheckAsync();

                                toolResult = JsonSerializer.Serialize(health);
                                stopwatch.Stop();

                                _logger.LogInformation(
                                    "AI tool execution succeeded: {ToolName}, Status: {Status}, DurationMs: {DurationMs}",
                                    functionCall.FunctionName,
                                    health.Status,
                                    stopwatch.ElapsedMilliseconds);
                            }
                            catch (Exception ex)
                            {
                                stopwatch.Stop();
                                _logger.LogError(
                                    ex,
                                    "AI tool execution failed: {ToolName}, DurationMs: {DurationMs}",
                                    functionCall.FunctionName,
                                    stopwatch.ElapsedMilliseconds);

                                toolResult = JsonSerializer.Serialize(
                                    new ApplicationHealthResult
                                    {
                                        Status = "Unhealthy",
                                        Timestamp = DateTime.UtcNow,
                                        Environment = _environment.EnvironmentName,
                                        Version = "1.0.0",
                                        DatabaseStatus = "Unknown",
                                        Message = "Application health check failed."
                                    });
                            }

                            inputItems.Add(
                                new FunctionCallOutputResponseItem(
                                    functionCall.CallId,
                                    toolResult));

                            toolWasCalled = true;
                            break;
                        }

                    default:
                        throw new InvalidOperationException(
                            $"Unknown function: {functionCall.FunctionName}");
                }
            }

            if (!toolWasCalled)
            {
                return response.Value.GetOutputText();
            }
        }
    }
}