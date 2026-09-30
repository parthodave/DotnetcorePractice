namespace DotNet8WebAPI.Model;

public sealed class ApplicationHealthResult
{
    public string Status { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public string Environment { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public string DatabaseStatus { get; init; } = string.Empty;
    public string? Message { get; init; }
}
