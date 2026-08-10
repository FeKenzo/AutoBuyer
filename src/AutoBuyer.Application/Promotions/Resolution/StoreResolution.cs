namespace AutoBuyer.Application.Promotions.Resolution;

public sealed record StoreResolution(
    string Name,
    string BaseUrl,
    bool IsKnown,
    bool RequiresReview = false);
