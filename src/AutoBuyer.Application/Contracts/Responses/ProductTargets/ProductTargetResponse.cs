namespace AutoBuyer.Application.Contracts.Responses.ProductTargets;

public sealed record ProductTargetResponse(
    Guid Id,
    Guid StoreId,
    string StoreName,
    string Name,
    string ProductUrl,
    decimal TargetPrice,
    bool AutoBuyEnabled,
    bool MonitoringEnabled,
    DateTime CreatedAt);