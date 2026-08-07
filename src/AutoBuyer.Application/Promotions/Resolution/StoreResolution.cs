namespace AutoBuyer.Application.Promotions.Resolution;

public sealed record StoreResolution(
    string Name,
    string BaseUrl,
    bool IsKnown,
    bool SupportsAutomaticMonitoring,
    bool RequiresReview = false);
