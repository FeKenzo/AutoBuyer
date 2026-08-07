namespace AutoBuyer.Application.Notifications;

public interface IPriceAlertNotifier
{
    Task NotifyAsync(
        PriceAlertNotification notification,
        CancellationToken cancellationToken);
}