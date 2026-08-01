using AutoBuyer.Application.Monitoring;
using AutoBuyer.Infrastructure.Monitoring.Parsing;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace AutoBuyer.Infrastructure.Monitoring.Extractors;

public sealed class PichauProductPriceExtractor
    : IStorePriceExtractor
{
    private readonly ILogger<PichauProductPriceExtractor> _logger;

    public PichauProductPriceExtractor(
        ILogger<PichauProductPriceExtractor> logger)
    {
        _logger = logger;
    }

    public int Priority => 100;

    public bool CanHandle(Uri productUri)
    {
        return productUri.Host.Equals(
                   "pichau.com.br",
                   StringComparison.OrdinalIgnoreCase)
               || productUri.Host.EndsWith(
                   ".pichau.com.br",
                   StringComparison.OrdinalIgnoreCase);
    }

    public async Task<ProductPriceResult> ExtractAsync(
        IPage page,
        Uri productUri,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var selectorPrice = await TryReadSpecificPriceAsync(page);

        if (selectorPrice.HasValue)
        {
            _logger.LogInformation(
                "Preço à vista da Pichau identificado por seletor específico: {Price}.",
                selectorPrice.Value);

            return ProductPriceResult.Found(selectorPrice.Value);
        }

        var contextualPrice = await TryReadContextualPriceAsync(page);

        if (contextualPrice.HasValue)
        {
            _logger.LogInformation(
                "Preço à vista da Pichau identificado pelo bloco PIX: {Price}.",
                contextualPrice.Value);

            return ProductPriceResult.Found(contextualPrice.Value);
        }

        return ProductPriceResult.Failed(
            "O preço à vista/PIX da Pichau não foi encontrado.");
    }

    private static async Task<decimal?> TryReadSpecificPriceAsync(
        IPage page)
    {
        var selectors = new[]
        {
            "[class*='price_vista-extraSpacePriceVista']",
            "[class*='price_vista'][class*='PriceVista']",
            "[class*='price_vista']"
        };

        foreach (var selector in selectors)
        {
            var locator = page.Locator(selector);
            var count = Math.Min(await locator.CountAsync(), 10);

            for (var index = 0; index < count; index++)
            {
                var item = locator.Nth(index);

                if (!await item.IsVisibleAsync())
                    continue;

                var text = await item.InnerTextAsync();
                var price = PriceParser.ParseBrazilianPrice(text);

                if (IsPlausibleProductPrice(price))
                    return price;
            }
        }

        return null;
    }

    private static async Task<decimal?> TryReadContextualPriceAsync(
        IPage page)
    {
        var labels = page.GetByText(
            "no PIX",
            new PageGetByTextOptions
            {
                Exact = false
            });

        var count = Math.Min(await labels.CountAsync(), 10);

        for (var index = 0; index < count; index++)
        {
            var label = labels.Nth(index);

            var containers = new[]
            {
                label.Locator("xpath=.."),
                label.Locator("xpath=../.."),
                label.Locator("xpath=../../..")
            };

            foreach (var container in containers)
            {
                if (await container.CountAsync() == 0)
                    continue;

                var text = await container.InnerTextAsync();
                var prices = ExtractPrices(text);

                if (prices.Count == 0)
                    continue;

                /*
                 * No bloco principal podem aparecer:
                 * - preço anterior;
                 * - preço à vista;
                 * - valor parcelado.
                 *
                 * O preço à vista tende a ser o menor preço total
                 * plausível, mas ignoramos parcelas muito pequenas.
                 */
                var cashPrice = prices
                    .Where(price => price >= 100m)
                    .OrderBy(price => price)
                    .FirstOrDefault();

                if (cashPrice > 0)
                    return cashPrice;
            }
        }

        return null;
    }

    private static List<decimal> ExtractPrices(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        var matches = System.Text.RegularExpressions.Regex.Matches(
            text,
            @"R\$\s*\d{1,3}(?:\.\d{3})*,\d{2}",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        return matches
            .Select(match =>
                PriceParser.ParseBrazilianPrice(match.Value))
            .Where(price => price.HasValue)
            .Select(price => price!.Value)
            .Distinct()
            .ToList();
    }

    private static bool IsPlausibleProductPrice(decimal? price)
    {
        return price is > 1m and < 1_000_000m;
    }
}