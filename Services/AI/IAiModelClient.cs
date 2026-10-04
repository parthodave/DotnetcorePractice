#pragma warning disable OPENAI001

using OpenAI.Responses;

namespace DotNet8WebAPI.Services.AI;

public interface IAiModelClient
{
    string ProviderName { get; }
    string ModelName { get; }

    Task<AiModelTurn> CreateResponseAsync(
        IReadOnlyList<ResponseItem> inputItems,
        IReadOnlyList<ResponseTool> tools,
        CancellationToken cancellationToken);
}

#pragma warning restore OPENAI001
