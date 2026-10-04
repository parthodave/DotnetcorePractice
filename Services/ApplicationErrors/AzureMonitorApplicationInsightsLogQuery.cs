using Azure.Monitor.Query;
using Azure.Monitor.Query.Models;
using System.Linq;

namespace DotNet8WebAPI.Services.ApplicationErrors;

public sealed class AzureMonitorApplicationInsightsLogQuery : IApplicationInsightsLogQuery
{
    private const string ErrorEventsQuery = """
        union isfuzzy=true
        (
            AppExceptions
            | project TimeGenerated, Severity = "Error", Operation = OperationName, Message = OuterMessage
        ),
        (
            AppRequests
            | where Success == false
            | project TimeGenerated, Severity = "Error", Operation = Name, Message = Name
        ),
        (
            AppDependencies
            | where Success == false
            | project TimeGenerated, Severity = "Error", Operation = Name, Message = Name
        ),
        (
            AppTraces
            | where SeverityLevel >= 3
            | project TimeGenerated, Severity = case(SeverityLevel >= 4, "Critical", "Error"), Operation = OperationName, Message
        )
        """;

    private readonly LogsQueryClient _client;

    public AzureMonitorApplicationInsightsLogQuery(LogsQueryClient client)
    {
        _client = client;
    }

    public async Task<ApplicationErrorQueryResult> QueryRecentErrorsAsync(
        string workspaceId,
        int windowMinutes,
        int maxResults,
        CancellationToken cancellationToken)
    {
        var timeRange = new QueryTimeRange(TimeSpan.FromMinutes(windowMinutes));
        var batch = new LogsBatchQuery();
        var countQueryId = batch.AddWorkspaceQuery(
            workspaceId,
            $"{ErrorEventsQuery} | summarize ErrorCount = count()",
            timeRange);
        var errorsQueryId = batch.AddWorkspaceQuery(
            workspaceId,
            $"{ErrorEventsQuery} | project TimeGenerated, TimestampUtc = tostring(TimeGenerated), Severity, Operation = tostring(Operation), Message = substring(tostring(Message), 0, 500) | top {maxResults} by TimeGenerated desc | project-away TimeGenerated",
            timeRange);

        var response = await _client.QueryBatchAsync(batch, cancellationToken);
        var errorCount = response.Value.GetResult<long>(countQueryId).SingleOrDefault();
        var items = response.Value
            .GetResult<ApplicationErrorQueryItem>(errorsQueryId)
            .ToList();

        return new ApplicationErrorQueryResult
        {
            ErrorCount = errorCount,
            Items = items
        };
    }
}
