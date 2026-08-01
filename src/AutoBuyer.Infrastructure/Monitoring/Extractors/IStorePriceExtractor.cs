using AutoBuyer.Application.Monitoring;
using Microsoft.Playwright;

namespace AutoBuyer.Infrastructure.Monitoring.Extractors;

public interface IStorePriceExtractor
{
    int Priority { get; }

    bool CanHandle(Uri productUri);

    Task<ProductPriceResult> ExtractAsync(
        IPage page,
        Uri productUri,
        CancellationToken cancellationToken);
}