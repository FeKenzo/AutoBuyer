using AutoBuyer.Domain.Enums;

namespace AutoBuyer.Domain.Entities;

public sealed class PromotionCandidate : Entity
{
    private const int MaximumProductNameLength = 300;
    private const int MaximumUrlLength = 2_000;
    private const int MaximumCouponLength = 500;
    private const int MaximumConditionsLength = 2_000;
    private const int MaximumOriginalMessageLength = 10_000;
    private const int MaximumReviewReasonLength = 1_000;

    private PromotionCandidate()
    {
        // Necessário para o Entity Framework.
    }

    public PromotionCandidate(
        long telegramChatId,
        int telegramMessageId,
        string productName,
        decimal advertisedPrice,
        string originalUrl,
        string originalMessage,
        string? coupon = null,
        string? conditions = null)
    {
        if (telegramChatId == 0)
        {
            throw new ArgumentException(
                "O identificador do chat do Telegram é obrigatório.",
                nameof(telegramChatId));
        }

        if (telegramMessageId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(telegramMessageId),
                "O identificador da mensagem deve ser maior que zero.");
        }

        SetProductName(productName);
        SetAdvertisedPrice(advertisedPrice);
        SetOriginalUrl(originalUrl);
        SetOriginalMessage(originalMessage);
        SetCoupon(coupon);
        SetConditions(conditions);

        TelegramChatId = telegramChatId;
        TelegramMessageId = telegramMessageId;
        Status = PromotionCandidateStatus.Pending;
        ReceivedAt = DateTime.UtcNow;
    }

    public long TelegramChatId { get; private set; }

    public int TelegramMessageId { get; private set; }

    public Guid? StoreId { get; private set; }

    public Store? Store { get; private set; }

    public string ProductName { get; private set; } = string.Empty;

    public decimal AdvertisedPrice { get; private set; }

    public string OriginalUrl { get; private set; } = string.Empty;

    public string? ResolvedUrl { get; private set; }

    public string? Coupon { get; private set; }

    public string? Conditions { get; private set; }

    public string OriginalMessage { get; private set; } = string.Empty;

    public PromotionCandidateStatus Status { get; private set; }

    public string? ReviewReason { get; private set; }

    public Guid? ProductTargetId { get; private set; }

    public ProductTarget? ProductTarget { get; private set; }

    public DateTime ReceivedAt { get; private set; }

    public DateTime? ProcessedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }

    public void AssignStore(Store store)
    {
        ArgumentNullException.ThrowIfNull(store);

        Store = store;
        StoreId = store.Id;
    }

    public void SetResolvedUrl(string resolvedUrl)
    {
        if (!Uri.TryCreate(
                resolvedUrl,
                UriKind.Absolute,
                out var uri)
            || uri.Scheme is not ("http" or "https"))
        {
            throw new ArgumentException(
                "A URL resolvida é inválida.",
                nameof(resolvedUrl));
        }

        ResolvedUrl = Limit(
            uri.AbsoluteUri,
            MaximumUrlLength);
    }

    public void UpdateFromTelegramMessage(
        string productName,
        decimal advertisedPrice,
        string originalUrl,
        string originalMessage,
        string? coupon,
        string? conditions)
    {
        SetProductName(productName);
        SetAdvertisedPrice(advertisedPrice);
        SetOriginalUrl(originalUrl);
        SetOriginalMessage(originalMessage);
        SetCoupon(coupon);
        SetConditions(conditions);

        Store = null;
        StoreId = null;
        ResolvedUrl = null;
        ProductTarget = null;
        ProductTargetId = null;
        Status = PromotionCandidateStatus.Pending;
        ReviewReason = null;
        ProcessedAt = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsNeedsReview(string? reason = null)
    {
        Status = PromotionCandidateStatus.NeedsReview;
        ReviewReason = LimitOptional(
            reason,
            MaximumReviewReasonLength);
        ProcessedAt = DateTime.UtcNow;
    }

    public void MarkAsUnsupportedStore(string? reason = null)
    {
        Status = PromotionCandidateStatus.UnsupportedStore;
        ReviewReason = LimitOptional(
            reason,
            MaximumReviewReasonLength);
        ProcessedAt = DateTime.UtcNow;
    }

    public void Ignore()
    {
        if (Status == PromotionCandidateStatus.Imported)
        {
            throw new InvalidOperationException(
                "Uma promoção importada não pode ser ignorada.");
        }

        Status = PromotionCandidateStatus.Ignored;
        ProcessedAt = DateTime.UtcNow;
    }

    public void MarkAsImported(ProductTarget productTarget)
    {
        ArgumentNullException.ThrowIfNull(productTarget);

        ProductTarget = productTarget;
        ProductTargetId = productTarget.Id;
        Status = PromotionCandidateStatus.Imported;
        ReviewReason = null;
        ProcessedAt = DateTime.UtcNow;
    }

    private void SetProductName(string productName)
    {
        if (string.IsNullOrWhiteSpace(productName))
        {
            throw new ArgumentException(
                "O nome do produto é obrigatório.",
                nameof(productName));
        }

        ProductName = Limit(
            productName.Trim(),
            MaximumProductNameLength);
    }

    private void SetAdvertisedPrice(decimal advertisedPrice)
    {
        if (advertisedPrice <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(advertisedPrice),
                "O preço anunciado deve ser maior que zero.");
        }

        AdvertisedPrice = advertisedPrice;
    }

    private void SetOriginalUrl(string originalUrl)
    {
        if (!Uri.TryCreate(
                originalUrl,
                UriKind.Absolute,
                out var uri)
            || uri.Scheme is not ("http" or "https"))
        {
            throw new ArgumentException(
                "A URL original é inválida.",
                nameof(originalUrl));
        }

        OriginalUrl = Limit(
            uri.AbsoluteUri,
            MaximumUrlLength);
    }

    private void SetOriginalMessage(string originalMessage)
    {
        if (string.IsNullOrWhiteSpace(originalMessage))
        {
            throw new ArgumentException(
                "A mensagem original é obrigatória.",
                nameof(originalMessage));
        }

        OriginalMessage = Limit(
            originalMessage.Trim(),
            MaximumOriginalMessageLength);
    }

    private void SetCoupon(string? coupon)
    {
        Coupon = string.IsNullOrWhiteSpace(coupon)
            ? null
            : Limit(coupon.Trim(), MaximumCouponLength);
    }

    private void SetConditions(string? conditions)
    {
        Conditions = string.IsNullOrWhiteSpace(conditions)
            ? null
            : Limit(conditions.Trim(), MaximumConditionsLength);
    }

    private static string Limit(
        string value,
        int maximumLength)
    {
        return value.Length <= maximumLength
            ? value
            : value[..maximumLength];
    }

    private static string? LimitOptional(
        string? value,
        int maximumLength)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : Limit(value.Trim(), maximumLength);
    }
}
