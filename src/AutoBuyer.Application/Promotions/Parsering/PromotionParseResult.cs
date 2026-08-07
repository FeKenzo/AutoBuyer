public sealed record PromotionParseResult(
    bool Success,
    string? ProductName,
    decimal? AdvertisedPrice,
    string? Url,
    string? Coupon,
    string? Conditions,
    string? Error)
{
    public static PromotionParseResult Successful(
        string productName,
        decimal advertisedPrice,
        string url,
        string? coupon,
        string? conditions)
    {
        return new(
            true,
            productName,
            advertisedPrice,
            url,
            coupon,
            conditions,
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
            error);
    }
}