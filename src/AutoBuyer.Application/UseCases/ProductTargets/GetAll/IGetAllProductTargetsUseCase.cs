using AutoBuyer.Application.Contracts.Responses.ProductTargets;

namespace AutoBuyer.Application.UseCases.ProductTargets.GetAll;

public interface IGetAllProductTargetsUseCase
{
    Task<IReadOnlyList<ProductTargetResponse>> ExecuteAsync(
        CancellationToken cancellationToken);
}