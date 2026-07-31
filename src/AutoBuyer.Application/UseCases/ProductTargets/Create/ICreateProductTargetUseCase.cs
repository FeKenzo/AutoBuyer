using AutoBuyer.Application.Contracts.Requests.ProductTargets;
using AutoBuyer.Application.Contracts.Responses.ProductTargets;

namespace AutoBuyer.Application.UseCases.ProductTargets.Create;

public interface ICreateProductTargetUseCase
{
    Task<ProductTargetResponse> ExecuteAsync(
        CreateProductTargetRequest request,
        CancellationToken cancellationToken);
}