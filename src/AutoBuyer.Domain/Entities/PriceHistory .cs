namespace AutoBuyer.Domain.Entities;

public sealed class PriceHistory : Entity
{
    private PriceHistory()
    {
        // Necessário para o Entity Framework.
    }

    public PriceHistory(
        Guid productTargetId,
        decimal price,
        bool isAvailable,
        DateTime capturedAt)
    {
        if (productTargetId == Guid.Empty)
            throw new ArgumentException(
                "O monitoramento é obrigatório.",
                nameof(productTargetId));

        if (price < 0)
            throw new ArgumentOutOfRangeException(
                nameof(price),
                "O preço não pode ser negativo.");

        ProductTargetId = productTargetId;
        Price = price;
        IsAvailable = isAvailable;
        CapturedAt = capturedAt;
    }

    public Guid ProductTargetId { get; private set; }

    public ProductTarget? ProductTarget { get; private set; }

    public decimal Price { get; private set; }

    public bool IsAvailable { get; private set; }

    public DateTime CapturedAt { get; private set; }
}