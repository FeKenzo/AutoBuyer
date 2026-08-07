using AutoBuyer.Application.Abstractions.Persistence;

namespace AutoBuyer.Application.UseCases.ProductTargets.ChangeMonitoringStatus;

public sealed class ChangeMonitoringStatusUseCase
    : IChangeMonitoringStatusUseCase
{
    private readonly IProductTargetRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public ChangeMonitoringStatusUseCase(
        IProductTargetRepository repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> ExecuteAsync(
        Guid id,
        bool enabled,
        CancellationToken cancellationToken)
    {
        var productTarget = await _repository.GetTrackedByIdAsync(
            id,
            cancellationToken);

        if (productTarget is null)
            return false;

        if (enabled)
            productTarget.EnableMonitoring();
        else
            productTarget.DisableMonitoring();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}