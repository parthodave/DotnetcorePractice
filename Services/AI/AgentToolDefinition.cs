using System.Text.Json;

namespace DotNet8WebAPI.Services.AI;

public sealed record AgentToolDefinition(
    string Name,
    string Description,
    BinaryData Parameters,
    ToolExecutionPolicy Policy,
    Func<JsonElement, CancellationToken, Task<object>> ExecuteAsync);
