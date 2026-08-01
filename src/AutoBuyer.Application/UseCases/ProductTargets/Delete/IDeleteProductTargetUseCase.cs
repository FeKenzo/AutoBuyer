namespace AutoBuyer.Application.UseCases.ProductTargets.Delete;

public interface IDeleteProductTargetUseCase
{
    Task<bool> ExecuteAsync(
        Guid id,
        CancellationToken cancellationToken);
}