namespace AutoBuyer.Application.Contracts.Requests.ProductTargets;

public sealed record CreateProductTargetRequest(
    Guid StoreId,
    string Name,
    string ProductUrl,
    decimal TargetPrice,
    bool AutoBuyEnabled);