namespace AutoBuyer.Application.Contracts.Responses.ProductTargets;

public sealed record ProductTargetResponse(
    Guid Id,
    Guid StoreId,
    string StoreName,
    string Name,
    string ProductUrl,
    string? ExternalProductId,
    decimal? TargetPrice,
    decimal? LastObservedPrice,
    DateTime? LastSeenAt,
    decimal? CurrentPrice,
    bool TargetReached,
    DateTime? LastCapturedAt,
    bool AutoBuyEnabled,
    bool MonitoringEnabled,
    DateTime CreatedAt,
    DateTime UpdatedAt);
