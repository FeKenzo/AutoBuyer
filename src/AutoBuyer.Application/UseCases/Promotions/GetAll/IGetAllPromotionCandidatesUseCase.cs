using AutoBuyer.Application.Contracts.Responses.Promotions;
using AutoBuyer.Domain.Enums;

namespace AutoBuyer.Application.UseCases.Promotions.GetAll;

public interface IGetAllPromotionCandidatesUseCase
{
    Task<IReadOnlyList<PromotionCandidateResponse>> ExecuteAsync(
        PromotionCandidateStatus? status,
        CancellationToken cancellationToken);
}