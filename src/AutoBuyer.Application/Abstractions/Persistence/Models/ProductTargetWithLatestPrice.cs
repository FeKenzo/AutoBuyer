namespace AutoBuyer.Application.Abstractions.Persistence.Models;

public sealed record ProductTargetWithLatestPrice(
    Guid Id,
    Guid StoreId,
    string StoreName,
    string Name,
    string ProductUrl,
    decimal TargetPrice,
    bool AutoBuyEnabled,
    bool MonitoringEnabled,
    DateTime CreatedAt,
    decimal? CurrentPrice,
    DateTime? LastCapturedAt);