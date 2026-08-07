using AutoBuyer.Application.Promotions.Parsing;

namespace AutoBuyer.Application.Tests.Promotions;

public sealed class TelegramPromotionParserTests
{
    private readonly TelegramPromotionParser _parser = new();

    [Fact]
    public void Parse_TerabyteTemplate_ExtractsAllRequiredFields()
    {
        const string message = """
            🟣 Terabyte
            🔥 Controle Gamer Ninja Sword V2 Rgb Bluetooth Pc Android Ios Switch Preto Gn Ct Swbynwv2
            ✅ R$ 109
            🔗 Link:
            https://www.terabyteshop.com.br/produto/38229/controle-gamer-ninja-sword-v2?p=2212992
            """;

        var result = _parser.Parse(message);

        Assert.True(result.Success);
        Assert.Equal("Terabyte", result.StoreName);
        Assert.Equal(
            "Controle Gamer Ninja Sword V2 Rgb Bluetooth Pc Android Ios Switch Preto Gn Ct Swbynwv2",
            result.ProductName);
        Assert.Equal(109m, result.AdvertisedPrice);
        Assert.False(result.NeedsReview);
    }

    [Fact]
    public void Parse_AmazonTemplate_SelectsPorInsteadOfDe()
    {
        const string message = """
            📦 Amazon
            🔥 LIMPEZA COMPLETA COM ESTAÇÃO AUTOLIMPANTE Robô Aspirador e Passa Pano eufy C20 Omni com Navegação a Laser
            De: R$ 3.561,00
            ✅ Por: R$ 3.204,90 🔥 9% OFF
            🔗 Link:
            https://www.amazon.com.br/dp/B0G676PTJG?tag=nescaautech-20
            ⚠ Preço, cupom e estoque podem mudar
            """;

        var result = _parser.Parse(message);

        Assert.True(result.Success);
        Assert.Equal("Amazon", result.StoreName);
        Assert.Equal(3_204.90m, result.AdvertisedPrice);
        Assert.Null(result.Conditions);
        Assert.False(result.NeedsReview);
    }

    [Fact]
    public void Parse_MultiplePaymentMethods_PrefersPixPrice()
    {
        const string message = """
            🟡 Mercado Livre
            🔥 CLÁSSICO QUE COMBINA COM TUDO Tênis Converse All Star CT AS Core Ox Original
            ✅ R$ 197,92
            💳 No Pix
            💵 R$ 207,92 - em outros meios💳
            🎟 Cupom: OFICIALMODA
            🔗 Link:
            https://meli.la/1qm8adK
            ⚠ Preço, cupom e estoque podem mudar.
            """;

        var result = _parser.Parse(message);

        Assert.True(result.Success);
        Assert.Equal(197.92m, result.AdvertisedPrice);
        Assert.Contains("Pix", result.Conditions!);
        Assert.False(result.NeedsReview);
    }

    [Fact]
    public void Parse_Coupon_DoesNotForceManualReview()
    {
        const string message = """
            🔵 Magalu
            🔥 Notebook Acer Aspire Go 15 AG15-71P-53GM Intel i5-13420H 8GB 512GB SSD 15.6 FHD TN Windows 11
            ✅ R$ 3.329
            💳 No Pix
            🎟 Cupom: 300PAYDAY
            🔗 Link:
            https://www.magazinevoce.com.br/magazinesnescautech/notebook-acer-aspire-go/p/240354400/in/nota
            ⚠ Preço, cupom e estoque podem mudar
            """;

        var result = _parser.Parse(message);

        Assert.True(result.Success);
        Assert.Equal("Magalu", result.StoreName);
        Assert.Equal(3_329m, result.AdvertisedPrice);
        Assert.Equal("300PAYDAY", result.Coupon);
        Assert.False(result.NeedsReview);
    }

    [Fact]
    public void Parse_MercadoLivreTemplate_ExtractsAdvertisedPriceAndCoupon()
    {
        const string message = """
            🟡 Mercado Livre
            🔥 RTX 5060 Placa de Vídeo INNO3D GeForce RTX5060 Twin X2 8GB GDDR7 128-bit
            ✅ R$ 2.002
            🎟 Cupom: MELIMAIS8008
            🔗 Link:
            https://meli.la/2h41khc
            ⚠ Preço, cupom e estoque podem mudar.
            """;

        var result = _parser.Parse(message);

        Assert.True(result.Success);
        Assert.Equal("Mercado Livre", result.StoreName);
        Assert.Equal(2_002m, result.AdvertisedPrice);
        Assert.Equal("MELIMAIS8008", result.Coupon);
        Assert.Null(result.Conditions);
        Assert.False(result.NeedsReview);
    }

    [Fact]
    public void Parse_InstallmentAmount_DoesNotReplaceTotalPrice()
    {
        const string message = """
            📦 Amazon
            🔥 Monitor Gamer 27 polegadas
            ✅ Por: R$ 2.499,90 em até 10x de R$ 249,99
            🔗 https://www.amazon.com.br/dp/B0ABCDEFGHI
            """;

        var result = _parser.Parse(message);

        Assert.True(result.Success);
        Assert.Equal(2_499.90m, result.AdvertisedPrice);
        Assert.False(result.NeedsReview);
    }

    [Fact]
    public void Parse_CompetingUnlabelledPrices_FlagsManualReview()
    {
        const string message = """
            🟢 Loja Exemplo
            🔥 Produto sem descrição das formas de pagamento
            ✅ R$ 100,00
            ✅ R$ 120,00
            🔗 https://loja.example/produto/1
            """;

        var result = _parser.Parse(message);

        Assert.True(result.Success);
        Assert.Equal(100m, result.AdvertisedPrice);
        Assert.True(result.NeedsReview);
    }
}
