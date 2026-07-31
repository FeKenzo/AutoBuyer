using AutoBuyer.Application.Contracts.Responses.ProductTargets;

namespace AutoBuyer.Application.UseCases.ProductTargets.GetById;

public interface IGetProductTargetByIdUseCase
{
    Task<ProductTargetResponse?> ExecuteAsync(
        Guid id,
        CancellationToken cancellationToken);
}