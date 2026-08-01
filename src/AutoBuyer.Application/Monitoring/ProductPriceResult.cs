namespace AutoBuyer.Application.Monitoring;

public sealed record ProductPriceResult(
    bool Success,
    decimal? Price,
    bool IsAvailable,
    string? Error)
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

    public static ProductPriceResult Failed(string error)
    {
        return new ProductPriceResult(
            false,
            null,
            false,
            error);
    }
}