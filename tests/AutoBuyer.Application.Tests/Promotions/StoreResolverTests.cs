using AutoBuyer.Application.Promotions.Resolution;

namespace AutoBuyer.Application.Tests.Promotions;

public sealed class StoreResolverTests
{
    private readonly StoreResolver _resolver = new();

    [Theory]
    [InlineData("Terabyte", "https://www.terabyteshop.com.br/produto/1/a")]
    [InlineData("Pichau", "https://www.pichau.com.br/produto-a")]
    [InlineData("Mercado Livre", "https://produto.mercadolivre.com.br/MLB-1")]
    [InlineData("Amazon", "https://www.amazon.com.br/dp/B0G676PTJG")]
    [InlineData("Magalu", "https://www.magazineluiza.com.br/produto/p/1")]
    [InlineData("Magalu", "https://www.magazinevoce.com.br/produto/p/1")]
    [InlineData("Shopee", "https://shopee.com.br/produto-i.1.2")]
    public void Resolve_KnownStores_UsesCanonicalDefinition(
        string expectedName,
        string url)
    {
        var result = _resolver.Resolve(null, url);

        Assert.NotNull(result);
        Assert.Equal(expectedName, result.Name);
        Assert.True(result.IsKnown);
    }

    [Fact]
    public void Resolve_UnknownStore_KeepsItImportable()
    {
        var result = _resolver.Resolve(
            "🟢 Loja Nova",
            "https://www.lojanova.com.br/produto/123");

        Assert.NotNull(result);
        Assert.Equal("Loja Nova", result.Name);
        Assert.False(result.IsKnown);
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
