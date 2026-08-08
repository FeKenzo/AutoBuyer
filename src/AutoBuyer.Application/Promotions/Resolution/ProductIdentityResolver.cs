using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace AutoBuyer.Application.Promotions.Resolution;

public sealed partial class ProductIdentityResolver
    : IProductIdentityResolver
{
    private static readonly HashSet<string> TrackingParameters =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "p",
            "ref",
            "ref_",
            "tag",
            "linkCode",
            "camp",
            "creative",
            "creativeASIN",
            "smid",
            "source",
            "src",
            "partner_id",
            "seller_id",
            "srsltid",
            "gclid",
            "fbclid"
        };

    public ProductIdentity Resolve(
        StoreResolution store,
        string productUrl)
    {
        var canonicalUrl = Canonicalize(productUrl);
        var nativeId = ExtractNativeId(
            store.Name,
            canonicalUrl);

        if (!string.IsNullOrWhiteSpace(nativeId))
        {
            return new ProductIdentity(
                nativeId,
                canonicalUrl,
                true);
        }

        return new ProductIdentity(
            CreateUrlFingerprint(canonicalUrl),
            canonicalUrl,
            false);
    }

    private static string Canonicalize(string productUrl)
    {
        var uri = new Uri(productUrl, UriKind.Absolute);
        var builder = new UriBuilder(uri)
        {
            Fragment = string.Empty,
            Host = uri.Host.ToLowerInvariant()
        };

        var retainedParameters = ParseQuery(uri.Query)
            .Where(parameter =>
                !parameter.Key.StartsWith(
                    "utm_",
                    StringComparison.OrdinalIgnoreCase) &&
                !TrackingParameters.Contains(parameter.Key))
            .OrderBy(parameter => parameter.Key,
                StringComparer.OrdinalIgnoreCase)
            .ThenBy(parameter => parameter.Value,
                StringComparer.Ordinal)
            .Select(parameter =>
                $"{Uri.EscapeDataString(parameter.Key)}=" +
                Uri.EscapeDataString(parameter.Value))
            .ToArray();

        builder.Query = string.Join("&", retainedParameters);

        var canonical = builder.Uri.AbsoluteUri;

        if (builder.Uri.AbsolutePath != "/")
            canonical = canonical.TrimEnd('/');

        return canonical;
    }

    private static IEnumerable<KeyValuePair<string, string>>
        ParseQuery(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            yield break;

        foreach (var segment in query.TrimStart('?').Split(
                     '&',
                     StringSplitOptions.RemoveEmptyEntries))
        {
            var separatorIndex = segment.IndexOf('=');
            var key = separatorIndex >= 0
                ? segment[..separatorIndex]
                : segment;
            var value = separatorIndex >= 0
                ? segment[(separatorIndex + 1)..]
                : string.Empty;

            yield return new KeyValuePair<string, string>(
                Uri.UnescapeDataString(key.Replace('+', ' ')),
                Uri.UnescapeDataString(value.Replace('+', ' ')));
        }
    }

    private static string? ExtractNativeId(
        string storeName,
        string canonicalUrl)
    {
        var match = storeName switch
        {
            "Terabyte" => TerabyteIdRegex().Match(canonicalUrl),
            "Mercado Livre" => MercadoLivreIdRegex().Match(canonicalUrl),
            "Amazon" => AmazonAsinRegex().Match(canonicalUrl),
            "Magalu" => MagaluIdRegex().Match(canonicalUrl),
            "Shopee" => ShopeeIdRegex().Match(canonicalUrl),
            _ => null
        };

        if (match is null || !match.Success)
            return null;

        var id = match.Groups["id"].Value
            .Replace("-", string.Empty)
            .ToUpperInvariant();

        return string.IsNullOrWhiteSpace(id)
            ? null
            : id;
    }

    private static string CreateUrlFingerprint(string canonicalUrl)
    {
        var hash = SHA256.HashData(
            Encoding.UTF8.GetBytes(canonicalUrl));

        return $"URL-{Convert.ToHexString(hash)}";
    }

    [GeneratedRegex(
        @"/produto/(?<id>\d+)(?:/|$)",
        RegexOptions.IgnoreCase)]
    private static partial Regex TerabyteIdRegex();

    [GeneratedRegex(
        @"\b(?<id>MLB-?\d+)\b",
        RegexOptions.IgnoreCase)]
    private static partial Regex MercadoLivreIdRegex();

    [GeneratedRegex(
        @"/(?:dp|gp/product)/(?<id>[A-Z0-9]{10})(?:[/?]|$)",
        RegexOptions.IgnoreCase)]
    private static partial Regex AmazonAsinRegex();

    [GeneratedRegex(
        @"/(?:p|produto)/(?:[^/?#]+/)*(?<id>[a-z0-9]{6,})(?:[/?#]|$)",
        RegexOptions.IgnoreCase)]
    private static partial Regex MagaluIdRegex();

    [GeneratedRegex(
        @"(?:-i\.\d+\.|/product/\d+/)(?<id>\d+)(?:[/?#]|$)",
        RegexOptions.IgnoreCase)]
    private static partial Regex ShopeeIdRegex();
}
