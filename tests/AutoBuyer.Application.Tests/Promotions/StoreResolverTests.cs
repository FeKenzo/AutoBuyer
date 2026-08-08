using AutoBuyer.Application.Promotions.Resolution;

namespace AutoBuyer.Application.Tests.Promotions;

public sealed class StoreResolverTests
{
    private readonly StoreResolver _resolver = new();

    [Theory]
    [InlineData("Terabyte", "https://www.terabyteshop.com.br/produto/1/a", true)]
    [InlineData("Pichau", "https://www.pichau.com.br/produto-a", true)]
    [InlineData("Mercado Livre", "https://produto.mercadolivre.com.br/MLB-1", false)]
    [InlineData("Amazon", "https://www.amazon.com.br/dp/B0G676PTJG", false)]
    [InlineData("Magalu", "https://www.magazineluiza.com.br/produto/p/1", false)]
    [InlineData("Magalu", "https://www.magazinevoce.com.br/produto/p/1", false)]
    [InlineData("Shopee", "https://shopee.com.br/produto-i.1.2", false)]
    public void Resolve_KnownStores_UsesCanonicalDefinition(
        string expectedName,
        string url,
        bool expectedMonitoringSupport)
    {
        var result = _resolver.Resolve(null, url);

        Assert.NotNull(result);
        Assert.Equal(expectedName, result.Name);
        Assert.True(result.IsKnown);
        Assert.Equal(
            expectedMonitoringSupport,
            result.SupportsAutomaticMonitoring);
    }

    [Fact]
    public void Resolve_UnknownStore_KeepsItImportableButUnmonitored()
    {
        var result = _resolver.Resolve(
            "🟢 Loja Nova",
            "https://www.lojanova.com.br/produto/123");

        Assert.NotNull(result);
        Assert.Equal("Loja Nova", result.Name);
        Assert.False(result.IsKnown);
        Assert.False(result.SupportsAutomaticMonitoring);
    }

    [Fact]
    public void Resolve_ConflictingHintAndDomain_FlagsManualReview()
    {
        var result = _resolver.Resolve(
            "Amazon",
            "https://produto.mercadolivre.com.br/MLB-123456");

        Assert.NotNull(result);
        Assert.Equal("Mercado Livre", result.Name);
        Assert.True(result.RequiresReview);
    }
}
