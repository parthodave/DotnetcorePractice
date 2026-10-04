using DotNet8WebAPI.Model;
using Microsoft.ApplicationInsights;
using Microsoft.EntityFrameworkCore;

namespace DotNet8WebAPI.Services.ApplicationHealth;

public sealed class ApplicationHealthService : IApplicationHealthService
{
    private const string Version = "1.0.0";
    private const string DatabaseFailureMessage = "Database connectivity check failed.";

    private readonly OurHeroDbContext _dbContext;
    private readonly TelemetryClient _telemetryClient;
    private readonly ILogger<ApplicationHealthService> _logger;
    private readonly IHostEnvironment _environment;

    public ApplicationHealthService(
        OurHeroDbContext dbContext,
        TelemetryClient telemetryClient,
        ILogger<ApplicationHealthService> logger,
        IHostEnvironment environment)
    {
        _dbContext = dbContext;
        _telemetryClient = telemetryClient;
        _logger = logger;
        _environment = environment;
    }

    public async Task<ApplicationHealthResult> CheckAsync(
        CancellationToken cancellationToken = default)
    {
        var timestamp = DateTime.UtcNow;

        try
        {
            await _dbContext.Database.ExecuteSqlAsync($"SELECT 1", cancellationToken);

            _telemetryClient.TrackEvent("HealthCheckPassed");
            _logger.LogInformation(
                "Health check passed - Database connectivity verified");

            return new ApplicationHealthResult
            {
                Status = "Healthy",
                TimestampUtc = timestamp,
                Environment = _environment.EnvironmentName,
                Version = Version,
                DatabaseStatus = "Healthy",
                Message = "Database connectivity verified."
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Health check FAILED - Database connectivity issue");
            _telemetryClient.TrackEvent(
                "HealthCheckFailed",
                new Dictionary<string, string>
                {
                    ["Component"] = "Database"
                });

            return new ApplicationHealthResult
            {
                Status = "Unhealthy",
                TimestampUtc = DateTime.UtcNow,
                Environment = _environment.EnvironmentName,
                Version = Version,
                DatabaseStatus = "Unhealthy",
                Message = DatabaseFailureMessage
            };
        }
    }
}
