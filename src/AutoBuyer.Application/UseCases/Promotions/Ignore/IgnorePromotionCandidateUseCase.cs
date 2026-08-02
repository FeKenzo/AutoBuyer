using AutoBuyer.Application.Abstractions.Persistence;
using AutoBuyer.Domain.Enums;

namespace AutoBuyer.Application.UseCases.Promotions.Ignore;

public sealed class IgnorePromotionCandidateUseCase
    : IIgnorePromotionCandidateUseCase
{
    private readonly IPromotionCandidateRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public IgnorePromotionCandidateUseCase(
        IPromotionCandidateRepository repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> ExecuteAsync(
        Guid promotionId,
        CancellationToken cancellationToken)
    {
        var promotion = await _repository.GetByIdAsync(
            promotionId,
            cancellationToken);

        if (promotion is null)
            return false;

        if (promotion.Status == PromotionCandidateStatus.Imported)
        {
            throw new InvalidOperationException(
                "Uma promoção importada não pode ser ignorada.");
        }

        promotion.Ignore();

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return true;
    }
}