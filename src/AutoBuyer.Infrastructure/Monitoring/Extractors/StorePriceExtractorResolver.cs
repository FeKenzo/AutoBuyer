namespace AutoBuyer.Infrastructure.Monitoring.Extractors;

public sealed class StorePriceExtractorResolver
{
    private readonly IReadOnlyList<IStorePriceExtractor> _extractors;

    public StorePriceExtractorResolver(
        IEnumerable<IStorePriceExtractor> extractors)
    {
        _extractors = extractors
            .OrderByDescending(extractor => extractor.Priority)
            .ToList();
    }

    public IStorePriceExtractor Resolve(Uri productUri)
    {
        var extractor = _extractors.FirstOrDefault(
            candidate => candidate.CanHandle(productUri));

        return extractor
            ?? throw new InvalidOperationException(
                $"Nenhum extrator de preço atende ao domínio " +
                $"'{productUri.Host}'.");
    }
}