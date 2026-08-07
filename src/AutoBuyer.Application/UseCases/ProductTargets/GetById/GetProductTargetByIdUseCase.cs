using AutoBuyer.Application.Abstractions.Persistence;
using AutoBuyer.Application.Contracts.Responses.ProductTargets;

namespace AutoBuyer.Application.UseCases.ProductTargets.GetById;

public sealed class GetProductTargetByIdUseCase
    : IGetProductTargetByIdUseCase
{
    private readonly IProductTargetRepository _repository;

    public GetProductTargetByIdUseCase(
        IProductTargetRepository repository)
    {
        _repository = repository;
    }

    public async Task<ProductTargetResponse?> ExecuteAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var target =
            await _repository.GetByIdWithLatestPriceAsync(
                id,
                cancellationToken);

        if (target is null)
            return null;

        return new ProductTargetResponse(
            target.Id,
            target.StoreId,
            target.StoreName,
            target.Name,
            target.ProductUrl,
            target.ExternalProductId,
            target.TargetPrice,
            target.LastObservedPrice,
            target.LastSeenAt,
            target.CurrentPrice,
            target.TargetPrice.HasValue &&
            target.CurrentPrice.HasValue &&
            target.CurrentPrice.Value <= target.TargetPrice.Value,
            target.LastCapturedAt,
            target.AutoBuyEnabled,
            target.MonitoringEnabled,
            target.CreatedAt,
            target.UpdatedAt);
    }
}
