using AutoBuyer.Domain.Entities;

namespace AutoBuyer.Application.Tests.Domain;

public sealed class ProductTargetTests
{
    [Fact]
    public void Observe_UpdatesMarketDataWithoutOverwritingTargetPrice()
    {
        var target = new ProductTarget(
            Guid.NewGuid(),
            "Produto antigo",
            "https://loja.example/produto/1",
            targetPrice: 100m,
            externalProductId: "1",
            lastObservedPrice: 120m);

        var seenAt = DateTime.UtcNow.AddMinutes(1);

        target.Observe(
            "Produto atualizado",
            "https://loja.example/produto/1",
            80m,
            seenAt);

        Assert.Equal(100m, target.TargetPrice);
        Assert.Equal(80m, target.LastObservedPrice);
        Assert.Equal(seenAt, target.LastSeenAt);
        Assert.Equal("Produto atualizado", target.Name);
    }

    [Fact]
    public void Constructor_WithoutTargetPrice_DoesNotEnableAutoBuy()
    {
        var target = new ProductTarget(
            Guid.NewGuid(),
            "Produto",
            "https://loja.example/produto/1",
            targetPrice: null,
            autoBuyEnabled: true,
            externalProductId: "1",
            lastObservedPrice: 90m);

        Assert.Null(target.TargetPrice);
        Assert.False(target.AutoBuyEnabled);
    }
}
