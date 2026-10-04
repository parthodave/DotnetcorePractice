#pragma warning disable OPENAI001

namespace DotNet8WebAPI.Services.AI;

public interface IAgentToolExecutor
{
    IReadOnlyList<OpenAI.Responses.ResponseTool> Tools { get; }

    Task<string> ExecuteAsync(
        string toolName,
        string argumentsJson,
        string callId,
        CancellationToken cancellationToken);
}

#pragma warning restore OPENAI001
