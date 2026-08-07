using AutoBuyer.Application.Abstractions.Persistence;
using AutoBuyer.Application.Contracts.Requests.ProductTargets;
using AutoBuyer.Application.Contracts.Responses.ProductTargets;
using AutoBuyer.Domain.Entities;

namespace AutoBuyer.Application.UseCases.ProductTargets.Create;

public sealed class CreateProductTargetUseCase
    : ICreateProductTargetUseCase
{
    private readonly IProductTargetRepository _productTargetRepository;
    private readonly IStoreRepository _storeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateProductTargetUseCase(
        IProductTargetRepository productTargetRepository,
        IStoreRepository storeRepository,
        IUnitOfWork unitOfWork)
    {
        _productTargetRepository = productTargetRepository;
        _storeRepository = storeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ProductTargetResponse> ExecuteAsync(
        CreateProductTargetRequest request,
        CancellationToken cancellationToken)
    {
        var store = await _storeRepository.GetByIdAsync(
            request.StoreId,
            cancellationToken);

        if (store is null)
            throw new KeyNotFoundException("Loja não encontrada.");

        if (!store.IsEnabled)
            throw new InvalidOperationException(
                "A loja informada está desabilitada.");

        var productTarget = new ProductTarget(
            store.Id,
            request.Name,
            request.ProductUrl,
            request.TargetPrice,
            request.AutoBuyEnabled);

        await _productTargetRepository.AddAsync(
            productTarget,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(productTarget, store.Name);
    }

    private static ProductTargetResponse Map(
    ProductTarget productTarget,
    string storeName)
    {
        return new ProductTargetResponse(
            productTarget.Id,
            productTarget.StoreId,
            storeName,
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