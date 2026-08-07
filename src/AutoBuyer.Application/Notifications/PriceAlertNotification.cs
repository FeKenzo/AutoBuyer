namespace AutoBuyer.Application.Notifications;

public sealed record PriceAlertNotification(
    Guid ProductTargetId,
    string ProductName,
    string StoreName,
    string ProductUrl,
    decimal CurrentPrice,
    decimal TargetPrice,
    DateTime CapturedAt);