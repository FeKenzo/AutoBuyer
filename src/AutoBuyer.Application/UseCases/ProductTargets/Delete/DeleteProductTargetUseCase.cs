using AutoBuyer.Application.Abstractions.Persistence;

namespace AutoBuyer.Application.UseCases.ProductTargets.Delete;

public sealed class DeleteProductTargetUseCase
    : IDeleteProductTargetUseCase
{
    private readonly IProductTargetRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteProductTargetUseCase(
        IProductTargetRepository repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> ExecuteAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var productTarget = await _repository.GetTrackedByIdAsync(
            id,
            cancellationToken);

        if (productTarget is null)
            return false;

        _repository.Remove(productTarget);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}