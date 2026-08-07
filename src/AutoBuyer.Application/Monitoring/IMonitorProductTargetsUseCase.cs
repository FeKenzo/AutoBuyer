namespace AutoBuyer.Application.UseCases.Monitoring;

public interface IMonitorProductTargetsUseCase
{
    Task ExecuteAsync(CancellationToken cancellationToken);
}