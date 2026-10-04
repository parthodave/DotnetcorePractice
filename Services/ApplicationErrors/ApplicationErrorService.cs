using DotNet8WebAPI.Model;
using System.Globalization;
using System.Text.RegularExpressions;

namespace DotNet8WebAPI.Services.ApplicationErrors;

public sealed class ApplicationErrorService : IApplicationErrorService
{
    private const int DefaultWindowMinutes = 30;
    private const int DefaultMaxResults = 20;
    private const int MaxWindowMinutes = 120;
    private const int MaxResults = 25;
    private const string UnavailableMessage =
        "Recent application telemetry is unavailable. Configure ApplicationInsights:WorkspaceId and grant log-query access.";

    private static readonly Regex SecretPattern = new(
        @"(?i)(api[-_ ]?key|password|secret|token|authorization|connectionstring|accountkey|sharedaccesskey|sharedaccesssignature|client[-_ ]?secret|refresh[-_ ]?token)(\s*[:=]\s*)[^\s,;&]+",
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(100));
    private static readonly Regex ConnectionStringPattern = new(
        @"(?i)\b(?:server|data source)\s*=\s*[^\s;]+(?:;\s*[^\s;]+)*",
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(100));
    private static readonly Regex TokenPattern = new(
        @"\b(?:sk-[A-Za-z0-9_-]{12,}|eyJ[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]{10,})\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(100));
    private static readonly Regex QuerySecretPattern = new(
        @"(?i)([?&](?:sig|signature|token|key)=)[^&\s]+",
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(100));

    private readonly IConfiguration _configuration;
    private readonly IApplicationInsightsLogQuery _logQuery;
    private readonly ILogger<ApplicationErrorService> _logger;

    public ApplicationErrorService(
        IConfiguration configuration,
        IApplicationInsightsLogQuery logQuery,
        ILogger<ApplicationErrorService> logger)
    {
        _configuration = configuration;
        _logQuery = logQuery;
        _logger = logger;
    }

    public async Task<ApplicationErrorResult> GetRecentErrorsAsync(
        CancellationToken cancellationToken = default)
    {
        var workspaceId = _configuration["ApplicationInsights:WorkspaceId"];
        if (string.IsNullOrWhiteSpace(workspaceId))
        {
            return Unavailable(
                "Recent application telemetry is not configured. Set ApplicationInsights:WorkspaceId.");
        }

        var windowMinutes = ReadBoundedSetting(
            "ApplicationInsights:WindowMinutes",
            DefaultWindowMinutes,
            minimum: 1,
            maximum: MaxWindowMinutes);
        var maxResults = ReadBoundedSetting(
            "ApplicationInsights:MaxResults",
            DefaultMaxResults,
            minimum: 1,
            maximum: MaxResults);

        try
        {
            var result = await _logQuery.QueryRecentErrorsAsync(
                workspaceId,
                windowMinutes,
                maxResults,
                cancellationToken);

            var errors = result.Items
                .Take(maxResults)
                .Select(item => new ApplicationErrorItem
                {
                    TimestampUtc = DateTimeOffset.TryParse(
                        item.TimestampUtc,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                        out var timestamp)
                            ? timestamp.UtcDateTime
                            : DateTime.UnixEpoch,
                    Severity = Sanitize(item.Severity),
                    Operation = Sanitize(item.Operation),
                    Message = Sanitize(item.Message)
                })
                .ToArray();

            return new ApplicationErrorResult
            {
                Status = result.ErrorCount > 0 ? "Degraded" : "Healthy",
                WindowMinutes = windowMinutes,
                ErrorCount = (int)Math.Min(result.ErrorCount, int.MaxValue),
                ResultsTruncated = result.ErrorCount > errors.Length,
                Errors = errors
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query recent Application Insights errors.");
            return Unavailable(UnavailableMessage, windowMinutes);
        }
    }

    private int ReadBoundedSetting(string key, int defaultValue, int minimum, int maximum)
    {
        return int.TryParse(_configuration[key], out var configuredValue)
            ? Math.Clamp(configuredValue, minimum, maximum)
            : defaultValue;
    }

    private static string Sanitize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var sanitized = ConnectionStringPattern.Replace(value, "[REDACTED_CONNECTION_STRING]");
        sanitized = SecretPattern.Replace(sanitized, "$1$2[REDACTED]");
        sanitized = QuerySecretPattern.Replace(sanitized, "$1[REDACTED]");
        sanitized = TokenPattern.Replace(sanitized, "[REDACTED_TOKEN]");
        return sanitized.Length <= 500 ? sanitized : sanitized[..500];
    }

    private static ApplicationErrorResult Unavailable(string message, int windowMinutes = DefaultWindowMinutes)
    {
        return new ApplicationErrorResult
        {
            Status = "Unavailable",
            WindowMinutes = windowMinutes,
            ErrorCount = 0,
            ResultsTruncated = false,
            Message = message
        };
    }
}
