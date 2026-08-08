using AutoBuyer.Application.Promotions.Resolution;

namespace AutoBuyer.Application.Tests.Promotions;

public sealed class ProductIdentityResolverTests
{
    private readonly ProductIdentityResolver _resolver = new();

    [Fact]
    public void Resolve_Terabyte_UsesProductIdAndRemovesAffiliateParameter()
    {
        var store = new StoreResolution(
            "Terabyte",
            "https://www.terabyteshop.com.br",
            true,
            true);

        var result = _resolver.Resolve(
            store,
            "https://www.terabyteshop.com.br/produto/38229/controle?p=2212992&utm_source=telegram");

        Assert.Equal("38229", result.ExternalProductId);
        Assert.Equal(
            "https://www.terabyteshop.com.br/produto/38229/controle",
            result.CanonicalUrl);
        Assert.True(result.IsStoreNativeId);
    }

    [Fact]
    public void Resolve_Amazon_UsesAsin()
    {
        var store = new StoreResolution(
            "Amazon",
            "https://www.amazon.com.br",
            true,
            false);

        var result = _resolver.Resolve(
            store,
            "https://www.amazon.com.br/dp/B0G676PTJG?tag=afiliado-20");

        Assert.Equal("B0G676PTJG", result.ExternalProductId);
        Assert.Equal(
            "https://www.amazon.com.br/dp/B0G676PTJG",
            result.CanonicalUrl);
    }

    [Fact]
    public void Resolve_UnknownStore_CreatesStableUrlFingerprint()
    {
        var store = new StoreResolution(
            "Loja Exemplo",
            "https://loja.example",
            false,
            false);

        var first = _resolver.Resolve(
            store,
            "https://loja.example/produtos/abc?utm_source=telegram");
        var second = _resolver.Resolve(
            store,
            "https://loja.example/produtos/abc");

        Assert.Equal(first.ExternalProductId, second.ExternalProductId);
        Assert.StartsWith("URL-", first.ExternalProductId);
        Assert.False(first.IsStoreNativeId);
    }
}
