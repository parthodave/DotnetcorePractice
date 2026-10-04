#pragma warning disable OPENAI001

using OpenAI.Responses;

namespace DotNet8WebAPI.Services.AI;

public sealed record AiModelTurn(
    IReadOnlyList<ResponseItem> OutputItems,
    string OutputText);

#pragma warning restore OPENAI001
