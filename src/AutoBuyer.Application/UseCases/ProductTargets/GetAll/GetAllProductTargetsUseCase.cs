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
        var targets = await _repository.GetAllAsync(cancellationToken);

        return targets
            .Select(target => new ProductTargetResponse(
                target.Id,
                target.StoreId,
                target.Store?.Name ?? string.Empty,
                target.Name,
                target.ProductUrl,
                target.TargetPrice,
                target.AutoBuyEnabled,
                target.MonitoringEnabled,
                target.CreatedAt))
            .ToList();
    }
}