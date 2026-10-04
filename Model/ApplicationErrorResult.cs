namespace DotNet8WebAPI.Model;

public sealed class ApplicationErrorResult
{
    public string Status { get; init; } = string.Empty;
    public int WindowMinutes { get; init; }
    public int ErrorCount { get; init; }
    public bool ResultsTruncated { get; init; }
    public IReadOnlyList<ApplicationErrorItem> Errors { get; init; } = [];
    public string? Message { get; init; }
}
