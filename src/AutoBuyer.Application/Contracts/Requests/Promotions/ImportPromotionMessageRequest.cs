namespace AutoBuyer.Application.Contracts.Requests.Promotions;

public sealed record ImportPromotionMessageRequest(
    long TelegramChatId,
    int TelegramMessageId,
    string Message,
    bool IsEdited = false);
