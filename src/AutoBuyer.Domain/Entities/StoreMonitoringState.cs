using AutoBuyer.Domain.Enums;

namespace AutoBuyer.Domain.Entities;

public sealed class StoreMonitoringState : Entity
{
    private StoreMonitoringState()
    {
        // Necessário para o Entity Framework.
    }

    public StoreMonitoringState(string host)
    {
        SetHost(host);

        Status = StoreMonitoringStatus.Supported;
        ConsecutiveFailures = 0;
        UpdatedAt = DateTime.UtcNow;
    }

    public string Host { get; private set; } = string.Empty;

    public StoreMonitoringStatus Status { get; private set; }

    public int ConsecutiveFailures { get; private set; }

    public int? LastHttpStatusCode { get; private set; }

    public string? LastError { get; private set; }

    public DateTime? LastSuccessAt { get; private set; }

    public DateTime? LastFailureAt { get; private set; }

    public DateTime? NextAllowedAttemptAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public bool CanExecute(DateTime utcNow)
    {
        if (Status is StoreMonitoringStatus.Unsupported
            or StoreMonitoringStatus.RequiresManualAction
            or StoreMonitoringStatus.RequiresSession)
        {
            return false;
        }

        return NextAllowedAttemptAt is null ||
               NextAllowedAttemptAt <= utcNow;
    }

    public void RegisterSuccess(DateTime utcNow)
    {
        Status = StoreMonitoringStatus.Supported;
        ConsecutiveFailures = 0;
        LastHttpStatusCode = null;
        LastError = null;
        LastSuccessAt = utcNow;
        NextAllowedAttemptAt = null;
        UpdatedAt = utcNow;
    }

    public void RegisterFailure(
        string error,
        int? httpStatusCode,
        DateTime utcNow)
    {
        ConsecutiveFailures++;
        LastHttpStatusCode = httpStatusCode;
        LastError = Limit(error, 2_000);
        LastFailureAt = utcNow;
        UpdatedAt = utcNow;

        if (RequiresManualAction(httpStatusCode, error))
        {
            Status = StoreMonitoringStatus.RequiresManualAction;
            NextAllowedAttemptAt = null;
            return;
        }

        Status = StoreMonitoringStatus.TemporarilyBlocked;
        NextAllowedAttemptAt = utcNow.Add(
            CalculateBackoff(ConsecutiveFailures));
    }

    public void MarkAsRequiresManualAction(
        string reason,
        DateTime utcNow)
    {
        Status = StoreMonitoringStatus.RequiresManualAction;
        LastError = Limit(reason, 2_000);
        LastFailureAt = utcNow;
        NextAllowedAttemptAt = null;
        UpdatedAt = utcNow;
    }

    public void MarkAsUnsupported(
        string reason,
        DateTime utcNow)
    {
        Status = StoreMonitoringStatus.Unsupported;
        LastError = Limit(reason, 2_000);
        NextAllowedAttemptAt = null;
        UpdatedAt = utcNow;
    }

    public void Enable(DateTime utcNow)
    {
        Status = StoreMonitoringStatus.Supported;
        ConsecutiveFailures = 0;
        LastHttpStatusCode = null;
        LastError = null;
        NextAllowedAttemptAt = null;
        UpdatedAt = utcNow;
    }

    private void SetHost(string host)
    {
        if (string.IsNullOrWhiteSpace(host))
            throw new ArgumentException(
                "O domínio da loja é obrigatório.",
                nameof(host));

        Host = host.Trim().ToLowerInvariant();
    }

    private static TimeSpan CalculateBackoff(int failures)
    {
        return failures switch
        {
            1 => TimeSpan.FromMinutes(15),
            2 => TimeSpan.FromHours(1),
            3 => TimeSpan.FromHours(4),
            4 => TimeSpan.FromHours(12),
            _ => TimeSpan.FromHours(24)
        };
    }

    private static bool RequiresManualAction(
        int? httpStatusCode,
        string error)
    {
        if (httpStatusCode == 403 &&
            (
                error.Contains(
                    "captcha",
                    StringComparison.OrdinalIgnoreCase)
                ||
                error.Contains(
                    "challenge",
                    StringComparison.OrdinalIgnoreCase)
                ||
                error.Contains(
                    "verificação",
                    StringComparison.OrdinalIgnoreCase)
            ))
        {
            return true;
        }

        return error.Contains(
                   "captcha",
                   StringComparison.OrdinalIgnoreCase)
               ||
               error.Contains(
                   "cloudflare",
                   StringComparison.OrdinalIgnoreCase)
               ||
               error.Contains(
                   "turnstile",
                   StringComparison.OrdinalIgnoreCase);
    }

    private static string Limit(string value, int maximumLength)
    {
        if (value.Length <= maximumLength)
            return value;

        return value[..maximumLength];
    }
}