using DotNet8WebAPI.Model;

namespace DotNet8WebAPI.Services.ApplicationHealth;

public interface IApplicationHealthService
{
    Task<ApplicationHealthResult> CheckAsync();
}
