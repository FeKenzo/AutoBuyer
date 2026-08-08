namespace AutoBuyer.Application.Promotions.Parsing;

public sealed record PromotionParseResult(
    bool Success,
    string? StoreName,
    string? ProductName,
    decimal? AdvertisedPrice,
    string? Url,
    string? Coupon,
    string? Conditions,
    bool NeedsReview,
    string? Error)
{
    public static PromotionParseResult Successful(
        string? storeName,
        string productName,
        decimal advertisedPrice,
        string url,
        string? coupon,
        string? conditions,
        bool needsReview = false)
    {
        return new(
            true,
            storeName,
            productName,
            advertisedPrice,
            url,
            coupon,
            conditions,
            needsReview,
            null);
    }

    public static PromotionParseResult Failed(string error)
    {
        return new(
            false,
            null,
            null,
            null,
            null,
            null,
            null,
            true,
            error);
    }
}
