using System.Globalization;
using System.Text.RegularExpressions;

namespace AutoBuyer.Infrastructure.Monitoring.Parsing;

public static partial class PriceParser
{
    public static decimal? ParseBrazilianPrice(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var match = BrazilianPriceRegex().Match(value);

        if (!match.Success)
            return null;

        var normalized = match.Groups["price"].Value
            .Replace(".", string.Empty, StringComparison.Ordinal)
            .Replace(",", ".", StringComparison.Ordinal)
            .Trim();

        return decimal.TryParse(
            normalized,
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out var price)
            ? price
            : null;
    }

    public static decimal? ParseFlexiblePrice(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var match = FlexiblePriceRegex().Match(value);

        if (!match.Success)
            return null;

        var capturedValue = match.Groups["price"].Value.Trim();

        if (capturedValue.Contains(','))
        {
            var normalized = capturedValue
                .Replace(".", string.Empty, StringComparison.Ordinal)
                .Replace(",", ".", StringComparison.Ordinal);

            if (decimal.TryParse(
                normalized,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var brazilianPrice))
            {
                return brazilianPrice;
            }
        }

        if (capturedValue.Contains('.') &&
            decimal.TryParse(
                capturedValue,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var invariantPrice))
        {
            return invariantPrice;
        }

        if (decimal.TryParse(
            capturedValue,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var integerPrice))
        {
            return NormalizeCents(integerPrice);
        }

        return null;
    }

    public static decimal NormalizeCents(decimal price)
    {
        var isInteger = decimal.Truncate(price) == price;

        return isInteger && price >= 100_000m
            ? price / 100m
            : price;
    }

    [GeneratedRegex(
        @"R\$\s*(?<price>\d{1,3}(?:\.\d{3})*,\d{2}|\d+,\d{2})",
        RegexOptions.IgnoreCase)]
    private static partial Regex BrazilianPriceRegex();

    [GeneratedRegex(
        @"(?:R\$\s*)?(?<price>\d{1,3}(?:\.\d{3})+,\d{2}|\d+,\d{2}|\d+\.\d{2}|\d{4,})",
        RegexOptions.IgnoreCase)]
    private static partial Regex FlexiblePriceRegex();
}