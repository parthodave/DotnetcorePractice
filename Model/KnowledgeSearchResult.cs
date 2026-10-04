namespace DotNet8WebAPI.Model;

public sealed class KnowledgeSearchResult
{
    public string Status { get; init; } = string.Empty;
    public IReadOnlyList<KnowledgeCitation> Citations { get; init; } = [];
    public string? Message { get; init; }
}
