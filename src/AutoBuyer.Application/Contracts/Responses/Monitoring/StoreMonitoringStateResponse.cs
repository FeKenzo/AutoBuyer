using AutoBuyer.Domain.Enums;

namespace AutoBuyer.Application.Contracts.Responses.Monitoring;

public sealed record StoreMonitoringStateResponse(
    string Host,
    StoreMonitoringStatus Status,
    int ConsecutiveFailures,
    int? LastHttpStatusCode,
    string? LastError,
    DateTime? LastSuccessAt,
    DateTime? LastFailureAt,
    DateTime? NextAllowedAttemptAt,
    DateTime UpdatedAt);