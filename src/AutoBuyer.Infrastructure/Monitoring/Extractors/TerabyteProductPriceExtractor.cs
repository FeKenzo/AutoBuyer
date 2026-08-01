using System.Text.Json;
using System.Text.RegularExpressions;
using AutoBuyer.Application.Monitoring;
using AutoBuyer.Infrastructure.Monitoring.Parsing;
using Microsoft.Playwright;

namespace AutoBuyer.Infrastructure.Monitoring.Extractors;

public sealed partial class TerabyteProductPriceExtractor
    : IStorePriceExtractor
{
    public int Priority => 100;

    public bool CanHandle(Uri productUri)
    {
        return productUri.Host.Contains(
            "terabyteshop.com.br",
            StringComparison.OrdinalIgnoreCase);
    }

    public async Task<ProductPriceResult> ExtractAsync(
        IPage page,
        Uri productUri,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var html = await page.ContentAsync();

        var jsonLdPrice = TryReadJsonLdPrice(html);

        if (jsonLdPrice.HasValue)
            return ProductPriceResult.Found(jsonLdPrice.Value);

        var metaPrice = await TryReadMetaPriceAsync(page);

        if (metaPrice.HasValue)
            return ProductPriceResult.Found(metaPrice.Value);

        return ProductPriceResult.Failed(
            "Nenhum preço válido foi encontrado na Terabyte.");
    }

    private static async Task<decimal?> TryReadMetaPriceAsync(
        IPage page)
    {
        var selectors = new[]
        {
            "meta[property='product:price:amount']",
            "meta[itemprop='price']",
            "meta[property='og:price:amount']"
        };

        foreach (var selector in selectors)
        {
            var locator = page.Locator(selector).First;

            if (await locator.CountAsync() == 0)
                continue;

            var content = await locator.GetAttributeAsync("content");
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

            if (string.IsNullOrWhiteSpace(json))
                continue;

            try
            {
                using var document = JsonDocument.Parse(json);

                var price = FindProductPrice(document.RootElement);

                if (price.HasValue)
                    return price;
            }
            catch (JsonException)
            {
                // Tenta o próximo bloco JSON-LD.
            }
        }

        return null;
    }

    private static decimal? FindProductPrice(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                var price = FindProductPrice(item);

                if (price.HasValue)
                    return price;
            }

            return null;
        }

        if (element.ValueKind != JsonValueKind.Object)
            return null;

        if (IsProductSchema(element) &&
            element.TryGetProperty("offers", out var offers))
        {
            var offerPrice = ReadOffersPrice(offers);

            if (offerPrice.HasValue)
                return offerPrice;
        }

        if (element.TryGetProperty("@graph", out var graph))
            return FindProductPrice(graph);

        return null;
    }

    private static decimal? ReadOffersPrice(JsonElement offers)
    {
        if (offers.ValueKind == JsonValueKind.Array)
        {
            foreach (var offer in offers.EnumerateArray())
            {
                var price = ReadOfferPrice(offer);

                if (price.HasValue)
                    return price;
            }

            return null;
        }

        return ReadOfferPrice(offers);
    }

    private static decimal? ReadOfferPrice(JsonElement offer)
    {
        if (offer.ValueKind != JsonValueKind.Object)
            return null;

        if (offer.TryGetProperty("price", out var price))
            return ParseJsonPrice(price);

        if (offer.TryGetProperty("lowPrice", out var lowPrice))
            return ParseJsonPrice(lowPrice);

        return null;
    }

    private static decimal? ParseJsonPrice(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Number &&
            element.TryGetDecimal(out var numericPrice))
        {
            return PriceParser.NormalizeCents(numericPrice);
        }

        return element.ValueKind == JsonValueKind.String
            ? PriceParser.ParseFlexiblePrice(element.GetString())
            : null;
    }

    private static bool IsProductSchema(JsonElement element)
    {
        if (!element.TryGetProperty("@type", out var type))
            return false;

        if (type.ValueKind == JsonValueKind.String)
        {
            return string.Equals(
                type.GetString(),
                "Product",
                StringComparison.OrdinalIgnoreCase);
        }

        return type.ValueKind == JsonValueKind.Array &&
               type.EnumerateArray().Any(item =>
                   item.ValueKind == JsonValueKind.String &&
                   string.Equals(
                       item.GetString(),
                       "Product",
                       StringComparison.OrdinalIgnoreCase));
    }

    [GeneratedRegex(
        @"<script[^>]+type\s*=\s*[""']application/ld\+json[""'][^>]*>(?<json>.*?)</script>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex JsonLdScriptRegex();
}