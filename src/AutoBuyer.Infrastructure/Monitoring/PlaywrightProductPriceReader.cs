using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using AutoBuyer.Application.Monitoring;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace AutoBuyer.Infrastructure.Monitoring;

public sealed partial class PlaywrightProductPriceReader
    : IProductPriceReader
{
    private readonly ILogger<PlaywrightProductPriceReader> _logger;

    public PlaywrightProductPriceReader(
        ILogger<PlaywrightProductPriceReader> logger)
    {
        _logger = logger;
    }

    public async Task<ProductPriceResult> ReadAsync(
        string productUrl,
        CancellationToken cancellationToken)
    {
        try
        {
            using var playwright = await Playwright.CreateAsync();

            await using var browser = await playwright.Chromium.LaunchAsync(
                new BrowserTypeLaunchOptions
                {
                    Headless = false,
                    SlowMo = 100
                });

            var context = await browser.NewContextAsync(
                new BrowserNewContextOptions
                {
                    Locale = "pt-BR",
                    TimezoneId = "America/Sao_Paulo",
                    ViewportSize = new ViewportSize
                    {
                        Width = 1366,
                        Height = 768
                    }
                });

            var page = await context.NewPageAsync();

            var response = await page.GotoAsync(
                productUrl,
                new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.DOMContentLoaded,
                    Timeout = 45_000
                });

            if (response is null)
            {
                return ProductPriceResult.Failed(
                    "O navegador não recebeu uma resposta da página.");
            }

            if (response.Status == 403)
            {
                return ProductPriceResult.Failed(
                    "A loja recusou o acesso ao navegador com HTTP 403.");
            }

            if (!response.Ok)
            {
                return ProductPriceResult.Failed(
                    $"A página retornou HTTP {response.Status}.");
            }

            await page.WaitForTimeoutAsync(2_000);

            var html = await page.ContentAsync();

            var price = TryReadJsonLdPrice(html)
                ?? await TryReadMetaPriceAsync(page);

            if (!price.HasValue)
            {
                return ProductPriceResult.Failed(
                    "A página abriu, mas nenhum preço foi identificado.");
            }

            return ProductPriceResult.Found(price.Value);
        }
        catch (PlaywrightException exception)
        {
            _logger.LogWarning(
                exception,
                "O Playwright não conseguiu consultar {ProductUrl}.",
                productUrl);

            return ProductPriceResult.Failed(exception.Message);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Erro inesperado ao consultar {ProductUrl}.",
                productUrl);

            return ProductPriceResult.Failed(exception.Message);
        }
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

            var price = ParsePrice(content);

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

                var price = FindPrice(document.RootElement);

                if (price.HasValue)
                    return price;
            }
            catch (JsonException)
            {
                // Ignora blocos JSON-LD inválidos.
            }
        }

        return null;
    }

    private static decimal? FindPrice(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                var price = FindPrice(item);

                if (price.HasValue)
                    return price;
            }

            return null;
        }

        if (element.ValueKind != JsonValueKind.Object)
            return null;

        if (IsProductSchema(element))
        {
            var productPrice = FindProductOfferPrice(element);

            if (productPrice.HasValue)
                return productPrice;
        }

        // Alguns JSON-LD armazenam os objetos dentro de @graph.
        if (element.TryGetProperty("@graph", out var graph))
        {
            return FindPrice(graph);
        }

        return null;
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

        if (type.ValueKind == JsonValueKind.Array)
        {
            return type
                .EnumerateArray()
                .Any(item =>
                    item.ValueKind == JsonValueKind.String &&
                    string.Equals(
                        item.GetString(),
                        "Product",
                        StringComparison.OrdinalIgnoreCase));
        }

        return false;
    }

    private static decimal? FindProductOfferPrice(
        JsonElement product)
    {
        if (!product.TryGetProperty("offers", out var offers))
            return null;

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
            return NormalizeTerabytePrice(numericPrice);
        }

        if (element.ValueKind == JsonValueKind.String)
        {
            return ParsePrice(element.GetString());
        }

        return null;
    }

    private static decimal NormalizeTerabytePrice(decimal price)
    {
        var isInteger = decimal.Truncate(price) == price;

        // A Terabyte pode expor certos valores monetários em centavos.
        // Exemplo: 469999 representa R$ 4.699,99.
        if (isInteger && price >= 100_000m)
        {
            return price / 100m;
        }

        return price;
    }

    private static decimal? ParsePrice(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var match = PriceRegex().Match(value);

        if (!match.Success)
            return null;

        var capturedValue = match.Groups["price"].Value.Trim();

        // Formato brasileiro: 4.699,99
        if (capturedValue.Contains(','))
        {
            var brazilianValue = capturedValue
                .Replace(".", string.Empty)
                .Replace(",", ".");

            if (decimal.TryParse(
                brazilianValue,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var parsedBrazilianPrice))
            {
                return parsedBrazilianPrice;
            }
        }

        // Formato decimal: 4699.99
        if (capturedValue.Contains('.'))
        {
            if (decimal.TryParse(
                capturedValue,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var parsedInvariantPrice))
            {
                return parsedInvariantPrice;
            }
        }

        // Formato interno em centavos: 469999
        if (decimal.TryParse(
            capturedValue,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var integerPrice))
        {
            return NormalizeTerabytePrice(integerPrice);
        }

        return null;
    }

    [GeneratedRegex(
        @"<script[^>]+type\s*=\s*[""']application/ld\+json[""'][^>]*>(?<json>.*?)</script>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex JsonLdScriptRegex();

    [GeneratedRegex(
        @"(?:R\$\s*)?(?<price>\d{1,3}(?:\.\d{3})+,\d{2}|\d+,\d{2}|\d+\.\d{2}|\d{4,})",
        RegexOptions.IgnoreCase)]
    private static partial Regex PriceRegex();
}