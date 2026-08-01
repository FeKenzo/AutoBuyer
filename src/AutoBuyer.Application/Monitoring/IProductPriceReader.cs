namespace AutoBuyer.Application.Monitoring;

public interface IProductPriceReader
{
    Task<ProductPriceResult> ReadAsync(
        string productUrl,
        CancellationToken cancellationToken);
}