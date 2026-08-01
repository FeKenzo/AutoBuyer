namespace AutoBuyer.Application.Monitoring;

public sealed record ProductPriceResult(
    bool Success,
    decimal? Price,
    bool IsAvailable,
    string? Error,
    int? HttpStatusCode = null,
    bool RequiresManualAction = false)
{
    public static ProductPriceResult Found(
        decimal price,
        bool isAvailable = true)
    {
        return new ProductPriceResult(
            true,
            price,
            isAvailable,
            null);
    }

    public static ProductPriceResult Failed(
        string error,
        int? httpStatusCode = null,
        bool requiresManualAction = false)
    {
        return new ProductPriceResult(
            false,
            null,
            false,
            error,
            httpStatusCode,
            requiresManualAction);
    }
}