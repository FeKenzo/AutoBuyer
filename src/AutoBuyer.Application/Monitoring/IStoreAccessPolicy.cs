namespace AutoBuyer.Application.Monitoring;

public interface IStoreAccessPolicy
{
    Task<bool> CanExecuteAsync(
        Uri productUri,
        CancellationToken cancellationToken);

    Task RegisterSuccessAsync(
        Uri productUri,
        CancellationToken cancellationToken);

    Task RegisterFailureAsync(
        Uri productUri,
        ProductPriceResult result,
        CancellationToken cancellationToken);
}