using AutoBuyer.Application.Contracts.Requests.Promotions;

namespace AutoBuyer.Application.UseCases.Promotions.CreateProductTarget;

public interface ICreateProductTargetFromPromotionUseCase
{
    Task<CreateProductTargetFromPromotionResult> ExecuteAsync(
        Guid promotionId,
        CreateProductTargetFromPromotionRequest request,
        CancellationToken cancellationToken);
}