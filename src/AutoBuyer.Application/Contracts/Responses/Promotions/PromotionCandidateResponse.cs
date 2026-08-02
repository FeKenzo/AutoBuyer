using AutoBuyer.Domain.Enums;

namespace AutoBuyer.Application.Contracts.Responses.Promotions;

public sealed record PromotionCandidateResponse(
    Guid Id,
    long TelegramChatId,
    int TelegramMessageId,
    Guid? StoreId,
    string? StoreName,
    string ProductName,
    decimal AdvertisedPrice,
    string OriginalUrl,
    string? ResolvedUrl,
    string? Coupon,
    string? Conditions,
    PromotionCandidateStatus Status,
    Guid? ProductTargetId,
    DateTime ReceivedAt,
    DateTime? ProcessedAt);