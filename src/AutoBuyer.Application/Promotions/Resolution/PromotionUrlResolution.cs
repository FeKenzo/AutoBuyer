namespace AutoBuyer.Application.Promotions.Resolution;

public sealed record PromotionUrlResolution(
    bool Success,
    string OriginalUrl,
    string? ResolvedUrl,
    string? Error)
{
    public string EffectiveUrl => ResolvedUrl ?? OriginalUrl;

    public static PromotionUrlResolution Resolved(
        string originalUrl,
        string resolvedUrl)
    {
        return new(true, originalUrl, resolvedUrl, null);
    }

    public static PromotionUrlResolution Unchanged(
        string originalUrl)
    {
        return new(true, originalUrl, originalUrl, null);
    }

    public static PromotionUrlResolution Failed(
        string originalUrl,
        string error)
    {
        return new(false, originalUrl, null, error);
    }
}
