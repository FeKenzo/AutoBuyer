using AutoBuyer.Application.Abstractions.Persistence;
using AutoBuyer.Application.Contracts.Responses.ProductTargets;

namespace AutoBuyer.Application.UseCases.ProductTargets.GetAll;

public sealed class GetAllProductTargetsUseCase
    : IGetAllProductTargetsUseCase
{
    private readonly IProductTargetRepository _repository;

    public GetAllProductTargetsUseCase(
        IProductTargetRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<ProductTargetResponse>> ExecuteAsync(
        CancellationToken cancellationToken)
    {
        var targets =
            await _repository.GetAllWithLatestPriceAsync(
                cancellationToken);

        return targets
            .Select(target => new ProductTargetResponse(
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
                target.UpdatedAt))
            .ToList();
    }
}
