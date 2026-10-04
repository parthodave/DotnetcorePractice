using DotNet8WebAPI.Model;

namespace DotNet8WebAPI.Services.ApplicationErrors;

public interface IApplicationErrorService
{
    Task<ApplicationErrorResult> GetRecentErrorsAsync(
        CancellationToken cancellationToken = default);
}
