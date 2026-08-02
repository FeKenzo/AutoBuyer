namespace AutoBuyer.Application.Contracts.Requests.Promotions;

public sealed record CreateProductTargetFromPromotionRequest(
    Guid StoreId,
    decimal? TargetPrice,
    string? ProductUrl,
    bool AutoBuyEnabled = false);