namespace AutoBuyer.Domain.Entities;

public sealed class ProductTarget : Entity
{
    private ProductTarget()
    {
        // Necessário para o Entity Framework.
    }

    public ProductTarget(
        Guid storeId,
        string name,
        string productUrl,
        decimal targetPrice,
        bool autoBuyEnabled = false)
    {
        if (storeId == Guid.Empty)
            throw new ArgumentException("A loja é obrigatória.", nameof(storeId));

        SetName(name);
        SetProductUrl(productUrl);
        SetTargetPrice(targetPrice);

        StoreId = storeId;
        AutoBuyEnabled = autoBuyEnabled;
        MonitoringEnabled = true;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid StoreId { get; private set; }

    public Store? Store { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string ProductUrl { get; private set; } = string.Empty;

    public decimal TargetPrice { get; private set; }

    public bool AutoBuyEnabled { get; private set; }

    public bool MonitoringEnabled { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public void Rename(string name)
    {
        SetName(name);
    }

    public void ChangeProductUrl(string productUrl)
    {
        SetProductUrl(productUrl);
    }

    public void ChangeTargetPrice(decimal targetPrice)
    {
        SetTargetPrice(targetPrice);
    }

    public void EnableMonitoring()
    {
        MonitoringEnabled = true;
    }

    public void DisableMonitoring()
    {
        MonitoringEnabled = false;
    }

    public void EnableAutoBuy()
    {
        AutoBuyEnabled = true;
    }

    public void DisableAutoBuy()
    {
        AutoBuyEnabled = false;
    }

    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(
                "O nome do produto é obrigatório.",
                nameof(name));

        Name = name.Trim();
    }

    private void SetProductUrl(string productUrl)
    {
        if (!Uri.TryCreate(productUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp &&
             uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException(
                "A URL do produto é inválida.",
                nameof(productUrl));
        }

        ProductUrl = uri.AbsoluteUri;
    }

    private void SetTargetPrice(decimal targetPrice)
    {
        if (targetPrice <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(targetPrice),
                "O preço-alvo deve ser maior que zero.");

        TargetPrice = targetPrice;
    }
}