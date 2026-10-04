namespace DotNet8WebAPI.Services.ApplicationErrors;

public interface IApplicationInsightsLogQuery
{
    Task<ApplicationErrorQueryResult> QueryRecentErrorsAsync(
        string workspaceId,
        int windowMinutes,
        int maxResults,
        CancellationToken cancellationToken);
}
