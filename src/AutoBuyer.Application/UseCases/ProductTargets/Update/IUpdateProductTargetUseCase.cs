using AutoBuyer.Application.Contracts.Requests.ProductTargets;
using AutoBuyer.Application.Contracts.Responses.ProductTargets;

namespace AutoBuyer.Application.UseCases.ProductTargets.Update;

public interface IUpdateProductTargetUseCase
{
    Task<ProductTargetResponse?> ExecuteAsync(
        Guid id,
        UpdateProductTargetRequest request,
        CancellationToken cancellationToken);
}