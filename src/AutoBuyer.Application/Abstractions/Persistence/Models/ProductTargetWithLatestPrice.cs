namespace AutoBuyer.Application.Abstractions.Persistence.Models;

public sealed record ProductTargetWithLatestPrice(
    Guid Id,
    Guid StoreId,
    string StoreName,
    string Name,
    string ProductUrl,
    string? ExternalProductId,
    decimal? TargetPrice,
    decimal? LastObservedPrice,
    DateTime? LastSeenAt,
    bool AutoBuyEnabled,
    bool MonitoringEnabled,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    decimal? CurrentPrice,
    DateTime? LastCapturedAt);
