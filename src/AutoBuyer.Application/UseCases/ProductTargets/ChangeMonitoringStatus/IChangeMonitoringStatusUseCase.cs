namespace AutoBuyer.Application.UseCases.ProductTargets.ChangeMonitoringStatus;

public interface IChangeMonitoringStatusUseCase
{
    Task<bool> ExecuteAsync(
        Guid id,
        bool enabled,
        CancellationToken cancellationToken);
}