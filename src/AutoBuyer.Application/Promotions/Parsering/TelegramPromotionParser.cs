using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace AutoBuyer.Application.Promotions.Parsing;

public sealed partial class TelegramPromotionParser
    : IPromotionMessageParser
{
    private static readonly string[] StoreAliases =
    [
        "Terabyte",
        "Pichau",
        "Mercado Livre",
        "Amazon",
        "Magalu",
        "Magazine Luiza",
        "Shopee"
    ];

    public PromotionParseResult Parse(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return PromotionParseResult.Failed("Mensagem vazia.");
        }

        var lines = message
            .Split(
                ['\r', '\n'],
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToArray();

        var url = ExtractUrl(message);

        if (url is null)
        {
            return PromotionParseResult.Failed(
                "Nenhum link encontrado.");
        }

        var storeName = ExtractStoreName(lines);
        var priceSelection = SelectAdvertisedPrice(lines);

        if (priceSelection.Price is null)
        {
            return PromotionParseResult.Failed(
                "Nenhum preço válido foi encontrado.");
        }

        var productName = ExtractProductName(
            lines,
            storeName);

        if (productName is null)
        {
            return PromotionParseResult.Failed(
                "Não foi possível identificar o nome do produto.");
        }

        var coupon = ExtractCoupon(lines);
        var conditions = ExtractConditions(lines);

        return PromotionParseResult.Successful(
            storeName,
            productName,
            priceSelection.Price.Value,
            url,
            coupon,
            conditions,
            priceSelection.IsAmbiguous);
    }

    private static string? ExtractUrl(string message)
    {
        var match = UrlRegex().Match(message);

        if (!match.Success)
            return null;

        return match.Value.TrimEnd(
            '.', ',', ';', ':', ')', ']', '}');
    }

    private static string? ExtractStoreName(
        IReadOnlyList<string> lines)
    {
        foreach (var line in lines.Take(3))
        {
            var alias = StoreAliases.FirstOrDefault(store =>
                line.Contains(
                    store,
                    StringComparison.OrdinalIgnoreCase));

            if (alias is not null)
            {
                return alias.Equals(
                    "Magazine Luiza",
                    StringComparison.OrdinalIgnoreCase)
                    ? "Magalu"
                    : alias;
            }
        }

        if (lines.Count >= 2 &&
            !lines[0].Contains("R$", StringComparison.OrdinalIgnoreCase) &&
            !lines[0].Contains("http", StringComparison.OrdinalIgnoreCase) &&
            !lines[1].Contains("R$", StringComparison.OrdinalIgnoreCase) &&
            !lines[1].Contains("http", StringComparison.OrdinalIgnoreCase))
        {
            var possibleStore = LeadingDecorationRegex()
                .Replace(lines[0], string.Empty)
                .Trim();

            if (possibleStore.Length is >= 2 and <= 100)
                return possibleStore;
        }

        return null;
    }

    private static PriceSelection SelectAdvertisedPrice(
        IReadOnlyList<string> lines)
    {
        var candidates = new List<PriceCandidate>();

        for (var lineIndex = 0;
             lineIndex < lines.Count;
             lineIndex++)
        {
            var line = lines[lineIndex];
            var matches = PriceRegex()
                .Matches(line)
                .Cast<Match>()
                .ToArray();

            for (var matchIndex = 0;
                 matchIndex < matches.Length;
                 matchIndex++)
            {
                var match = matches[matchIndex];
                var parsedPrice = ParsePrice(
                    match.Groups["price"].Value);

                if (!parsedPrice.HasValue)
                    continue;

                var nextMatchIndex = matchIndex + 1 < matches.Length
                    ? matches[matchIndex + 1].Index
                    : line.Length;

                var prefix = line[..match.Index];
                var paymentDescription = line[
                    match.Index..nextMatchIndex];

                candidates.Add(new PriceCandidate(
                    parsedPrice.Value,
                    CalculatePriceScore(
                        prefix,
                        paymentDescription,
                        matches.Length == 1 && lineIndex > 0
                            ? lines[lineIndex - 1]
                            : null,
                        matches.Length == 1 && lineIndex + 1 < lines.Count
                            ? lines[lineIndex + 1]
                            : null),
                    lineIndex));
            }
        }

        if (candidates.Count == 0)
            return new(null, false);

        var highestScore = candidates.Max(candidate => candidate.Score);
        var best = candidates
            .Where(candidate => candidate.Score == highestScore)
            .OrderBy(candidate => candidate.LineIndex)
            .ToArray();

        var distinctBestPrices = best
            .Select(candidate => candidate.Price)
            .Distinct()
            .ToArray();

        return new(
            best[0].Price,
            distinctBestPrices.Length > 1);
    }

    private static int CalculatePriceScore(
        string prefix,
        string paymentDescription,
        string? previousLine,
        string? nextLine)
    {
        var normalizedPrefix = RemoveDiacritics(
                LeadingDecorationRegex().Replace(prefix, string.Empty))
            .ToLowerInvariant();
        var normalizedPayment = RemoveDiacritics(paymentDescription)
            .ToLowerInvariant();
        var normalizedPrevious = RemoveDiacritics(previousLine ?? string.Empty)
            .ToLowerInvariant();
        var normalizedNext = RemoveDiacritics(nextLine ?? string.Empty)
            .ToLowerInvariant();
        var score = 0;

        if (CurrentPriceLabelRegex().IsMatch(normalizedPrefix))
            score += 80;

        if (normalizedPayment.Contains("pix") ||
            normalizedPayment.Contains("a vista") ||
            IsPixCondition(normalizedNext))
        {
            score += 70;
        }

        if (normalizedPrefix.Contains("preco") ||
            normalizedPrefix.Contains("oferta") ||
            normalizedPrefix.Contains("agora") ||
            normalizedPrefix.Contains("a partir"))
        {
            score += 50;
        }

        if (normalizedPayment.Contains("com cupom"))
            score += 30;

        if (normalizedPayment.Contains("parcelado"))
            score += 10;

        if (normalizedPayment.Contains("outros meios") ||
            normalizedPayment.Contains("demais meios"))
        {
            score -= 20;
        }

        if (OldPriceLabelRegex().IsMatch(normalizedPrefix))
            score -= 100;

        if (InstallmentValuePrefixRegex().IsMatch(normalizedPrefix) ||
            normalizedPrevious.Contains("valor da parcela"))
        {
            score -= 200;
        }

        return score;
    }

    private static bool IsPixCondition(string normalizedLine)
    {
        var cleaned = LeadingDecorationRegex()
            .Replace(normalizedLine, string.Empty)
            .Trim();

        return cleaned.StartsWith("no pix", StringComparison.Ordinal) ||
               cleaned.StartsWith("via pix", StringComparison.Ordinal) ||
               cleaned.StartsWith("a vista", StringComparison.Ordinal);
    }

    private static string? ExtractProductName(
        IReadOnlyList<string> lines,
        string? storeName)
    {
        foreach (var line in lines)
        {
            if (IsMetadataLine(line, storeName))
                continue;

            var cleaned = LeadingDecorationRegex()
                .Replace(line, string.Empty)
                .Trim();

            if (cleaned.Length >= 3)
                return cleaned;
        }

        return null;
    }

    private static bool IsMetadataLine(
        string line,
        string? storeName)
    {
        if (line.Contains("R$", StringComparison.OrdinalIgnoreCase) ||
            line.StartsWith("http", StringComparison.OrdinalIgnoreCase) ||
            line.Contains("http://", StringComparison.OrdinalIgnoreCase) ||
            line.Contains("https://", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var normalized = RemoveDiacritics(line).ToLowerInvariant();

        if (!string.IsNullOrWhiteSpace(storeName) &&
            normalized.Contains(
                RemoveDiacritics(storeName).ToLowerInvariant()))
        {
            return true;
        }

        return normalized.StartsWith("cupom") ||
               normalized.StartsWith("link") ||
               normalized.StartsWith("acesse") ||
               normalized.StartsWith("compre") ||
               normalized.StartsWith("aproveite") ||
               normalized.StartsWith("frete") ||
               normalized.StartsWith("preco, cupom") ||
               normalized.Contains("estoque pode") ||
               normalized.Contains("precos podem") ||
               normalized.Contains("sujeito a alteracao");
    }

    private static string? ExtractCoupon(
        IReadOnlyList<string> lines)
    {
        foreach (var line in lines)
        {
            var match = CouponRegex().Match(line);

            if (!match.Success)
                continue;

            var coupon = match.Groups["coupon"].Value
                .Trim(' ', ':', '-', '`', '*');

            if (!string.IsNullOrWhiteSpace(coupon))
                return coupon;
        }

        return null;
    }

    private static string? ExtractConditions(
        IReadOnlyList<string> lines)
    {
        var conditions = lines
            .Where(IsRelevantCondition)
            .Select(line => LeadingDecorationRegex()
                .Replace(line, string.Empty)
                .Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return conditions.Length == 0
            ? null
            : string.Join(" | ", conditions);
    }

    private static bool IsRelevantCondition(string line)
    {
        var normalized = RemoveDiacritics(line).ToLowerInvariant();

        if (normalized.Contains("preco, cupom e estoque podem mudar") ||
            normalized.Contains("precos, cupons e estoque podem mudar"))
        {
            return false;
        }

        return normalized.Contains("frete") ||
               normalized.Contains("parcelado") ||
               normalized.Contains("pix") ||
               normalized.Contains("a vista") ||
               normalized.Contains("prime") ||
               normalized.Contains("cliente ouro");
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

    private static string RemoveDiacritics(string value)
    {
        var normalized = value.Normalize(
            NormalizationForm.FormD);

        return string.Concat(normalized.Where(character =>
            CharUnicodeInfo.GetUnicodeCategory(character)
                != UnicodeCategory.NonSpacingMark));
    }

    private sealed record PriceCandidate(
        decimal Price,
        int Score,
        int LineIndex);

    private sealed record PriceSelection(
        decimal? Price,
        bool IsAmbiguous);

    [GeneratedRegex(
        @"https?://[^\s\]\[<>()]+",
        RegexOptions.IgnoreCase)]
    private static partial Regex UrlRegex();

    [GeneratedRegex(
        @"R\$\s*(?<price>\d{1,3}(?:\.\d{3})*(?:,\d{2})?|\d+(?:,\d{2})?)",
        RegexOptions.IgnoreCase)]
    private static partial Regex PriceRegex();

    [GeneratedRegex(
        @"(?:^|\s)de\s*:?\s*$",
        RegexOptions.IgnoreCase)]
    private static partial Regex OldPriceLabelRegex();

    [GeneratedRegex(
        @"(?:\d{1,2}\s*x|parcela)\s*(?:de\s*)?$",
        RegexOptions.IgnoreCase)]
    private static partial Regex InstallmentValuePrefixRegex();

    [GeneratedRegex(
        @"(?:^|\s)por\s*:?\s*$",
        RegexOptions.IgnoreCase)]
    private static partial Regex CurrentPriceLabelRegex();

    [GeneratedRegex(
        @"cupom\s*:?\s*(?<coupon>[\p{L}\p{N}_-]+)",
        RegexOptions.IgnoreCase)]
    private static partial Regex CouponRegex();

    [GeneratedRegex(
        @"^[^\p{L}\p{N}]+",
        RegexOptions.None)]
    private static partial Regex LeadingDecorationRegex();
}
