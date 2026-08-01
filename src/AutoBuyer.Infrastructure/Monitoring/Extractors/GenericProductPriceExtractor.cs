using System.Text.Json;
using System.Text.RegularExpressions;
using AutoBuyer.Application.Monitoring;
using AutoBuyer.Infrastructure.Monitoring.Parsing;
using Microsoft.Playwright;

namespace AutoBuyer.Infrastructure.Monitoring.Extractors;

public sealed partial class GenericProductPriceExtractor
    : IStorePriceExtractor
{
    public int Priority => -100;

    public bool CanHandle(Uri productUri)
    {
        // Fallback para qualquer domínio não reconhecido.
        return true;
    }

    public async Task<ProductPriceResult> ExtractAsync(
        IPage page,
        Uri productUri,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var metaPrice = await TryReadMetaPriceAsync(page);

        if (metaPrice.HasValue)
            return ProductPriceResult.Found(metaPrice.Value);

        var html = await page.ContentAsync();
        var jsonLdPrice = TryReadJsonLdPrice(html);

        if (jsonLdPrice.HasValue)
            return ProductPriceResult.Found(jsonLdPrice.Value);

        return ProductPriceResult.Failed(
            $"Nenhum preço estruturado foi encontrado em " +
            $"'{productUri.Host}'.");
    }

    private static async Task<decimal?> TryReadMetaPriceAsync(
        IPage page)
    {
        var selectors = new[]
        {
            "meta[property='product:price:amount']",
            "meta[property='og:price:amount']",
            "meta[itemprop='price']",
            "[itemprop='price'][content]"
        };

        foreach (var selector in selectors)
        {
            var locator = page.Locator(selector).First;

            if (await locator.CountAsync() == 0)
                continue;

            var content =
                await locator.GetAttributeAsync("content")
                ?? await locator.TextContentAsync();

            var price = PriceParser.ParseFlexiblePrice(content);

            if (price.HasValue)
                return price;
        }

        return null;
    }

    private static decimal? TryReadJsonLdPrice(string html)
    {
        var matches = JsonLdScriptRegex().Matches(html);

        foreach (Match match in matches)
        {
            var json = match.Groups["json"].Value.Trim();

            try
            {
                using var document = JsonDocument.Parse(json);

                var price = FindOfferPrice(document.RootElement);

                if (price.HasValue)
                    return price;
            }
            catch (JsonException)
            {
                // Ignora JSON-LD inválido.
            }
        }

        return null;
    }

    private static decimal? FindOfferPrice(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                var price = FindOfferPrice(item);

                if (price.HasValue)
                    return price;
            }

            return null;
        }

        if (element.ValueKind != JsonValueKind.Object)
            return null;

        if (element.TryGetProperty("offers", out var offers))
        {
            var price = FindOfferPrice(offers);

            if (price.HasValue)
                return price;
        }

        if (element.TryGetProperty("price", out var priceElement))
        {
            var price = priceElement.ValueKind switch
            {
                JsonValueKind.Number
                    when priceElement.TryGetDecimal(out var number)
                    => number,

                JsonValueKind.String
                    => PriceParser.ParseFlexiblePrice(
                        priceElement.GetString()),

                _ => null
            };

            if (price.HasValue)
                return price;
        }

        foreach (var property in element.EnumerateObject())
        {
            var nestedPrice = FindOfferPrice(property.Value);

            if (nestedPrice.HasValue)
                return nestedPrice;
        }

        return null;
    }

    [GeneratedRegex(
        @"<script[^>]+type\s*=\s*[""']application/ld\+json[""'][^>]*>(?<json>.*?)</script>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex JsonLdScriptRegex();
}