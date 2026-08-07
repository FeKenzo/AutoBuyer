using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using AutoBuyer.Application.Monitoring;

namespace AutoBuyer.Infrastructure.Monitoring;

public sealed partial class StructuredDataProductPriceReader
    : IProductPriceReader
{
    private readonly HttpClient _httpClient;

    public StructuredDataProductPriceReader(
        HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ProductPriceResult> ReadAsync(
        string productUrl,
        CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                productUrl);

            request.Headers.TryAddWithoutValidation(
                "User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
                "AppleWebKit/537.36 (KHTML, like Gecko) " +
                "Chrome/122.0.0.0 Safari/537.36");

            request.Headers.TryAddWithoutValidation(
                "Accept-Language",
                "pt-BR,pt;q=0.9,en;q=0.8");

            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return ProductPriceResult.Failed(
                    $"A página retornou HTTP {(int)response.StatusCode}.");
            }

            var html = await response.Content.ReadAsStringAsync(
                cancellationToken);

            var jsonLdPrice = TryReadJsonLdPrice(html);

            if (jsonLdPrice.HasValue)
                return ProductPriceResult.Found(jsonLdPrice.Value);

            var metaPrice = TryReadMetaPrice(html);

            if (metaPrice.HasValue)
                return ProductPriceResult.Found(metaPrice.Value);

            return ProductPriceResult.Failed(
                "Nenhum preço estruturado foi encontrado na página.");
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return ProductPriceResult.Failed(
                "Tempo limite ao consultar a página.");
        }
        catch (HttpRequestException exception)
        {
            return ProductPriceResult.Failed(exception.Message);
        }
        catch (JsonException exception)
        {
            return ProductPriceResult.Failed(
                $"JSON-LD inválido: {exception.Message}");
        }
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
                // Alguns sites possuem blocos JSON-LD inválidos.
                // Tentamos o próximo bloco.
            }
        }

        return null;
    }

    private static decimal? FindPrice(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                {
                    foreach (var property in element.EnumerateObject())
                    {
                        if (property.NameEquals("price") ||
                            property.NameEquals("lowPrice"))
                        {
                            var parsedPrice =
                                ParsePrice(property.Value);

                            if (parsedPrice.HasValue)
                                return parsedPrice;
                        }
                    }

                    foreach (var property in element.EnumerateObject())
                    {
                        var nestedPrice = FindPrice(property.Value);

                        if (nestedPrice.HasValue)
                            return nestedPrice;
                    }

                    break;
                }

            case JsonValueKind.Array:
                {
                    foreach (var item in element.EnumerateArray())
                    {
                        var nestedPrice = FindPrice(item);

                        if (nestedPrice.HasValue)
                            return nestedPrice;
                    }

                    break;
                }
        }

        return null;
    }

    private static decimal? ParsePrice(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Number &&
            element.TryGetDecimal(out var numericPrice))
        {
            return numericPrice;
        }

        if (element.ValueKind != JsonValueKind.String)
            return null;

        return ParsePrice(element.GetString());
    }

    private static decimal? TryReadMetaPrice(string html)
    {
        var match = MetaPriceRegex().Match(html);

        if (!match.Success)
            return null;

        return ParsePrice(match.Groups["price"].Value);
    }

    private static decimal? ParsePrice(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var normalized = value
            .Replace("R$", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("\u00A0", string.Empty)
            .Trim();

        if (decimal.TryParse(
            normalized,
            NumberStyles.Number,
            CultureInfo.GetCultureInfo("pt-BR"),
            out var brazilianPrice))
        {
            return brazilianPrice;
        }

        if (decimal.TryParse(
            normalized,
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out var invariantPrice))
        {
            return invariantPrice;
        }

        return null;
    }

    [GeneratedRegex(
        @"<script[^>]+type\s*=\s*[""']application/ld\+json[""'][^>]*>(?<json>.*?)</script>",
        RegexOptions.IgnoreCase |
        RegexOptions.Singleline)]
    private static partial Regex JsonLdScriptRegex();

    [GeneratedRegex(
        @"(?:property|itemprop)\s*=\s*[""'](?:product:price:amount|price)[""'][^>]+content\s*=\s*[""'](?<price>[^""']+)[""']",
        RegexOptions.IgnoreCase |
        RegexOptions.Singleline)]
    private static partial Regex MetaPriceRegex();
}