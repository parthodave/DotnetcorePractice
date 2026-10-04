namespace DotNet8WebAPI.Model;

public sealed class ApplicationErrorItem
{
    public DateTime TimestampUtc { get; init; }
    public string Severity { get; init; } = string.Empty;
    public string Operation { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}
