namespace DotNet8WebAPI.Services.AI;

public interface IAiService
{
    Task<string> AskAsync(
        string message,
        CancellationToken cancellationToken = default,
        string? correlationId = null);
}