using AutoBuyer.Application.Abstractions.Persistence;
using AutoBuyer.Application.Contracts.Requests.ProductTargets;
using AutoBuyer.Application.Contracts.Responses.ProductTargets;

namespace AutoBuyer.Application.UseCases.ProductTargets.Update;

public sealed class UpdateProductTargetUseCase
    : IUpdateProductTargetUseCase
{
    private readonly IProductTargetRepository _repository;
    private readonly IStoreRepository _storeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProductTargetUseCase(
        IProductTargetRepository repository,
        IStoreRepository storeRepository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _storeRepository = storeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ProductTargetResponse?> ExecuteAsync(
        Guid id,
        UpdateProductTargetRequest request,
        CancellationToken cancellationToken)
    {
        var productTarget = await _repository.GetTrackedByIdAsync(
            id,
            cancellationToken);

        if (productTarget is null)
            return null;

        productTarget.Rename(request.Name);
        productTarget.ChangeProductUrl(request.ProductUrl);
        productTarget.ChangeTargetPrice(request.TargetPrice);

        if (request.AutoBuyEnabled)
            productTarget.EnableAutoBuy();
        else
            productTarget.DisableAutoBuy();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var store = await _storeRepository.GetByIdAsync(
            productTarget.StoreId,
            cancellationToken);

        return new ProductTargetResponse(
            productTarget.Id,
            productTarget.StoreId,
            store?.Name ?? string.Empty,
            productTarget.Name,
            productTarget.ProductUrl,
            productTarget.TargetPrice,
            null,
            false,
            null,
            productTarget.AutoBuyEnabled,
            productTarget.MonitoringEnabled,
            productTarget.CreatedAt);
    }
}