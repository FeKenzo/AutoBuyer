using System.Globalization;
using System.Text;

namespace AutoBuyer.Application.Promotions.Resolution;

public sealed class StoreResolver : IStoreResolver
{
    private static readonly StoreDefinition[] Definitions =
    [
        new(
            "Terabyte",
            "https://www.terabyteshop.com.br",
            ["terabyte", "terabyteshop"],
            ["terabyteshop.com.br"],
            true),
        new(
            "Pichau",
            "https://www.pichau.com.br",
            ["pichau"],
            ["pichau.com.br"],
            true),
        new(
            "Mercado Livre",
            "https://www.mercadolivre.com.br",
            ["mercado livre", "mercadolivre", "meli"],
            ["mercadolivre.com.br", "mercadolivre.com", "meli.la"],
            false),
        new(
            "Amazon",
            "https://www.amazon.com.br",
            ["amazon"],
            ["amazon.com.br", "amzn.to"],
            false),
        new(
            "Magalu",
            "https://www.magazineluiza.com.br",
            ["magalu", "magazine luiza", "magazineluiza"],
            [
                "magazineluiza.com.br",
                "magazinevoce.com.br",
                "magalu.com",
                "magalu.onelink.me"
            ],
            false),
        new(
            "Shopee",
            "https://shopee.com.br",
            ["shopee", "shoppe"],
            ["shopee.com.br", "s.shopee.com.br"],
            false)
    ];

    public StoreResolution? Resolve(
        string? storeHint,
        string productUrl)
    {
        if (!Uri.TryCreate(
                productUrl,
                UriKind.Absolute,
                out var uri))
        {
            return null;
        }

        var host = NormalizeHost(uri.Host);
        var definitionByHost = Definitions.FirstOrDefault(candidate =>
            candidate.Domains.Any(domain =>
                HostMatches(host, domain)));
        StoreDefinition? definitionByHint = null;

        if (!string.IsNullOrWhiteSpace(storeHint))
        {
            var normalizedHint = NormalizeText(storeHint);

            definitionByHint = Definitions.FirstOrDefault(candidate =>
                candidate.Aliases.Any(alias =>
                    normalizedHint.Contains(
                        NormalizeText(alias),
                        StringComparison.Ordinal)));
        }

        var definition = definitionByHost ?? definitionByHint;

        if (definition is not null)
        {
            var requiresReview = definitionByHost is null ||
                definitionByHint is not null &&
                !string.Equals(
                    definitionByHost.Name,
                    definitionByHint.Name,
                    StringComparison.OrdinalIgnoreCase);

            return new StoreResolution(
                definition.Name,
                definition.BaseUrl,
                true,
                definition.SupportsAutomaticMonitoring,
                requiresReview);
        }

        var unknownName = NormalizeUnknownStoreName(
            storeHint,
            host);

        return new StoreResolution(
            unknownName,
            uri.GetLeftPart(UriPartial.Authority),
            false,
            false);
    }

    private static string NormalizeUnknownStoreName(
        string? storeHint,
        string host)
    {
        if (!string.IsNullOrWhiteSpace(storeHint))
        {
            var cleaned = string.Concat(storeHint
                .Trim()
                .SkipWhile(character =>
                    !char.IsLetterOrDigit(character)))
                .Trim();

            if (!string.IsNullOrWhiteSpace(cleaned))
                return Limit(cleaned, 100);
        }

        var firstHostPart = host.Split('.')[0];

        return Limit(
            CultureInfo.InvariantCulture.TextInfo.ToTitleCase(
                firstHostPart.Replace('-', ' ')),
            100);
    }

    private static string NormalizeHost(string host)
    {
        return host.StartsWith("www.", StringComparison.OrdinalIgnoreCase)
            ? host[4..].ToLowerInvariant()
            : host.ToLowerInvariant();
    }

    private static bool HostMatches(
        string host,
        string domain)
    {
        var normalizedDomain = NormalizeHost(domain);

        return host.Equals(
                   normalizedDomain,
                   StringComparison.OrdinalIgnoreCase) ||
               host.EndsWith(
                   $".{normalizedDomain}",
                   StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeText(string value)
    {
        var decomposed = value.Normalize(
            NormalizationForm.FormD);

        return string.Concat(decomposed
                .Where(character =>
                    CharUnicodeInfo.GetUnicodeCategory(character)
                        != UnicodeCategory.NonSpacingMark))
            .ToLowerInvariant();
    }

    private static string Limit(
        string value,
        int maximumLength)
    {
        return value.Length <= maximumLength
            ? value
            : value[..maximumLength];
    }

    private sealed record StoreDefinition(
        string Name,
        string BaseUrl,
        string[] Aliases,
        string[] Domains,
        bool SupportsAutomaticMonitoring);
}
