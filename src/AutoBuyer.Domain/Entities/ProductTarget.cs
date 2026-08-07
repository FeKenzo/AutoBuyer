namespace AutoBuyer.Domain.Entities;

public sealed class ProductTarget : Entity
{
    private const int MaximumNameLength = 300;
    private const int MaximumUrlLength = 2_000;
    private const int MaximumExternalProductIdLength = 200;

    private ProductTarget()
    {
        // Necessário para o Entity Framework.
    }

    public ProductTarget(
        Guid storeId,
        string name,
        string productUrl,
        decimal? targetPrice,
        bool autoBuyEnabled = false,
        string? externalProductId = null,
        decimal? lastObservedPrice = null,
        bool monitoringEnabled = true)
    {
        if (storeId == Guid.Empty)
        {
            throw new ArgumentException(
                "A loja é obrigatória.",
                nameof(storeId));
        }

        SetName(name);
        SetProductUrl(productUrl);
        SetExternalProductId(externalProductId);

        if (targetPrice.HasValue)
            SetTargetPrice(targetPrice.Value);

        if (lastObservedPrice.HasValue)
            SetLastObservedPrice(lastObservedPrice.Value);

        StoreId = storeId;
        AutoBuyEnabled = autoBuyEnabled && targetPrice.HasValue;
        MonitoringEnabled = monitoringEnabled;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = CreatedAt;
        LastSeenAt = lastObservedPrice.HasValue
            ? CreatedAt
            : null;
    }

    public Guid StoreId { get; private set; }

    public Store? Store { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string ProductUrl { get; private set; } = string.Empty;

    public string? ExternalProductId { get; private set; }

    public decimal? TargetPrice { get; private set; }

    public decimal? LastObservedPrice { get; private set; }

    public DateTime? LastSeenAt { get; private set; }

    public bool AutoBuyEnabled { get; private set; }

    public bool MonitoringEnabled { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public void Rename(string name)
    {
        SetName(name);
        Touch();
    }

    public void ChangeProductUrl(string productUrl)
    {
        SetProductUrl(productUrl);
        Touch();
    }

    public void AssignExternalProductId(string externalProductId)
    {
        SetExternalProductId(externalProductId);
        Touch();
    }

    public void ChangeTargetPrice(decimal targetPrice)
    {
        SetTargetPrice(targetPrice);
        Touch();
    }

    public void Observe(
        string name,
        string productUrl,
        decimal observedPrice,
        DateTime seenAt)
    {
        SetName(name);
        SetProductUrl(productUrl);
        SetLastObservedPrice(observedPrice);

        LastSeenAt = seenAt.Kind == DateTimeKind.Utc
            ? seenAt
            : seenAt.ToUniversalTime();
        UpdatedAt = LastSeenAt.Value;
    }

    public void EnableMonitoring()
    {
        MonitoringEnabled = true;
        Touch();
    }

    public void DisableMonitoring()
    {
        MonitoringEnabled = false;
        Touch();
    }

    public void EnableAutoBuy()
    {
        if (!TargetPrice.HasValue)
        {
            throw new InvalidOperationException(
                "Defina um preço-alvo antes de habilitar a compra automática.");
        }

        AutoBuyEnabled = true;
        Touch();
    }

    public void DisableAutoBuy()
    {
        AutoBuyEnabled = false;
        Touch();
    }

    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "O nome do produto é obrigatório.",
                nameof(name));
        }

        Name = Limit(name.Trim(), MaximumNameLength);
    }

    private void SetProductUrl(string productUrl)
    {
        if (!Uri.TryCreate(
                productUrl,
                UriKind.Absolute,
                out var uri) ||
            uri.Scheme is not ("http" or "https"))
        {
            throw new ArgumentException(
                "A URL do produto é inválida.",
                nameof(productUrl));
        }

        ProductUrl = Limit(
            uri.AbsoluteUri,
            MaximumUrlLength);
    }

    private void SetExternalProductId(string? externalProductId)
    {
        ExternalProductId = string.IsNullOrWhiteSpace(externalProductId)
            ? null
            : Limit(
                externalProductId.Trim(),
                MaximumExternalProductIdLength);
    }

    private void SetTargetPrice(decimal targetPrice)
    {
        if (targetPrice <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(targetPrice),
                "O preço-alvo deve ser maior que zero.");
        }

        TargetPrice = targetPrice;
    }

    private void SetLastObservedPrice(decimal observedPrice)
    {
        if (observedPrice <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(observedPrice),
                "O último preço observado deve ser maior que zero.");
        }

        LastObservedPrice = observedPrice;
    }

    private void Touch()
    {
        UpdatedAt = DateTime.UtcNow;
    }

    private static string Limit(
        string value,
        int maximumLength)
    {
        return value.Length <= maximumLength
            ? value
            : value[..maximumLength];
    }
}
