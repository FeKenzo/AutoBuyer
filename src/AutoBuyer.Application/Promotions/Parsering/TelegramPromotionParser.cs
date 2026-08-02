using System.Globalization;
using System.Text.RegularExpressions;

namespace AutoBuyer.Application.Promotions.Parsing;

public sealed partial class TelegramPromotionParser
    : IPromotionMessageParser
{
    public PromotionParseResult Parse(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return PromotionParseResult.Failed(
                "Mensagem vazia.");
        }

        var url = UrlRegex().Match(message).Value;

        if (string.IsNullOrWhiteSpace(url))
        {
            return PromotionParseResult.Failed(
                "Nenhum link encontrado.");
        }

        var priceMatch = PriceRegex().Match(message);

        if (!priceMatch.Success)
        {
            return PromotionParseResult.Failed(
                "Nenhum preço encontrado.");
        }

        var price = ParsePrice(
            priceMatch.Groups["price"].Value);

        if (!price.HasValue)
        {
            return PromotionParseResult.Failed(
                "Preço inválido.");
        }

        var lines = message.Split(
            ['\r', '\n'],
            StringSplitOptions.RemoveEmptyEntries |
            StringSplitOptions.TrimEntries);

        var name = lines
            .FirstOrDefault(line =>
                !line.Contains("R$", StringComparison.OrdinalIgnoreCase)
                && !line.StartsWith("🎟", StringComparison.Ordinal)
                && !line.StartsWith("⚠", StringComparison.Ordinal)
                && !line.StartsWith("http", StringComparison.OrdinalIgnoreCase));

        name = name?
            .Replace("🔥", string.Empty, StringComparison.Ordinal)
            .Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            return PromotionParseResult.Failed(
                "Não foi possível identificar o nome.");
        }

        var coupon = CouponRegex()
            .Match(message)
            .Groups["coupon"]
            .Value
            .Trim();

        var conditions = lines
            .FirstOrDefault(line =>
                line.StartsWith("⚠", StringComparison.Ordinal));

        conditions = conditions?
            .Replace("⚠", string.Empty, StringComparison.Ordinal)
            .Trim();

        return PromotionParseResult.Successful(
            name,
            price.Value,
            url,
            string.IsNullOrWhiteSpace(coupon)
                ? null
                : coupon,
            conditions);
    }

    private static decimal? ParsePrice(string value)
    {
        var normalized = value
            .Replace(".", string.Empty)
            .Replace(",", ".")
            .Trim();

        return decimal.TryParse(
            normalized,
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out var price)
            ? price
            : null;
    }

    [GeneratedRegex(
        @"https?://[^\s]+",
        RegexOptions.IgnoreCase)]
    private static partial Regex UrlRegex();

    [GeneratedRegex(
        @"R\$\s*(?<price>\d{1,3}(?:\.\d{3})*(?:,\d{2})?|\d+(?:,\d{2})?)",
        RegexOptions.IgnoreCase)]
    private static partial Regex PriceRegex();

    [GeneratedRegex(
        @"(?:Cupom|Cupom:)\s*(?<coupon>[^\r\n]+)",
        RegexOptions.IgnoreCase)]
    private static partial Regex CouponRegex();
}