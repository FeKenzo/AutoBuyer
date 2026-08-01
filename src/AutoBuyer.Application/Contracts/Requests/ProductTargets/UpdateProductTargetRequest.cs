namespace AutoBuyer.Application.Contracts.Requests.ProductTargets;

public sealed record UpdateProductTargetRequest(
    string Name,
    string ProductUrl,
    decimal TargetPrice,
    bool AutoBuyEnabled);