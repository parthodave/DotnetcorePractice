using DotNet8WebAPI.Application.Books;
using DotNet8WebAPI.Services.ApplicationErrors;
using DotNet8WebAPI.Services.ApplicationHealth;
using DotNet8WebAPI.Services.Knowledge;
using System.Text.Json;

namespace DotNet8WebAPI.Services.AI;

public static class ApplicationAgentTools
{
    public static IReadOnlyList<AgentToolDefinition> Create(
        IBookService bookService,
        IApplicationHealthService healthService,
        IApplicationErrorService errorService,
        IKnowledgeService knowledgeService)
    {
        return
        [
            new AgentToolDefinition(
                "get_all_books",
                "Gets all books using the existing Books service and Service Bus read flow.",
                BinaryData.FromString("""
                {
                    "type": "object",
                    "properties": {},
                    "required": [],
                    "additionalProperties": false
                }
                """),
                ToolExecutionPolicy.ReadOnly,
                async (arguments, cancellationToken) =>
                {
                    RequireNoArguments(arguments);
                    return await bookService.GetAllBooks(cancellationToken);
                }),
            new AgentToolDefinition(
                "get_application_health",
                "Checks current application and SQL database health. Use for application health, status, availability, or database questions. Never infer health without this result.",
                BinaryData.FromString("""
                {
                    "type": "object",
                    "properties": {},
                    "required": [],
                    "additionalProperties": false
                }
                """),
                ToolExecutionPolicy.ReadOnly,
                async (arguments, cancellationToken) =>
                {
                    RequireNoArguments(arguments);
                    return await healthService.CheckAsync(cancellationToken);
                }),
            new AgentToolDefinition(
                "get_recent_application_errors",
                "Queries recent real Application Insights failures including exceptions, failed requests, dependencies, and error traces. Use for recent errors or investigating application problems. Reports Unavailable if telemetry querying is not configured.",
                BinaryData.FromString("""
                {
                    "type": "object",
                    "properties": {},
                    "required": [],
                    "additionalProperties": false
                }
                """),
                ToolExecutionPolicy.ReadOnly,
                async (arguments, cancellationToken) =>
                {
                    RequireNoArguments(arguments);
                    return await errorService.GetRecentErrorsAsync(cancellationToken);
                }),
            new AgentToolDefinition(
                "search_application_knowledge",
                "Searches the application's local architecture and operations Markdown documents. Use for questions about this repository. Base answers on returned excerpts and cite their source paths.",
                BinaryData.FromString("""
                {
                    "type": "object",
                    "properties": {
                        "query": {
                            "type": "string",
                            "description": "The application or architecture question to search for."
                        }
                    },
                    "required": ["query"],
                    "additionalProperties": false
                }
                """),
                ToolExecutionPolicy.ReadOnly,
                async (arguments, cancellationToken) =>
                {
                    if (!arguments.TryGetProperty("query", out var queryElement) ||
                        queryElement.ValueKind != JsonValueKind.String)
                    {
                        throw new JsonException("A string query is required.");
                    }

                    var query = queryElement.GetString();
                    if (string.IsNullOrWhiteSpace(query) || query.Length > 500)
                    {
                        throw new JsonException("The query must contain 1 to 500 characters.");
                    }

                    return await knowledgeService.SearchAsync(query, cancellationToken);
                })
        ];
    }

    private static void RequireNoArguments(JsonElement arguments)
    {
        if (arguments.EnumerateObject().Any())
        {
            throw new JsonException("This tool does not accept arguments.");
        }
    }
}
