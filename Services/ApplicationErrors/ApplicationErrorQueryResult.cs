namespace DotNet8WebAPI.Services.ApplicationErrors;

public sealed class ApplicationErrorQueryResult
{
    public long ErrorCount { get; init; }
    public IReadOnlyList<ApplicationErrorQueryItem> Items { get; init; } = [];
}

public sealed class ApplicationErrorQueryItem
{
    public string TimestampUtc { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public string Operation { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}
