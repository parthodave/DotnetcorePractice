using DotNet8WebAPI.Model;

namespace DotNet8WebAPI.Services.Knowledge;

public interface IKnowledgeService
{
    Task<KnowledgeSearchResult> SearchAsync(
        string query,
        CancellationToken cancellationToken = default);
}
