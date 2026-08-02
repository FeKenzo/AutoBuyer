namespace AutoBuyer.Application.UseCases.Promotions.Ignore;

public interface IIgnorePromotionCandidateUseCase
{
    Task<bool> ExecuteAsync(
        Guid promotionId,
        CancellationToken cancellationToken);
}