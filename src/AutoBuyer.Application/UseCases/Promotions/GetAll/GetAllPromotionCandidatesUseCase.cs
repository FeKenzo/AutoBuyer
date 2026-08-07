using AutoBuyer.Application.Abstractions.Persistence;
using AutoBuyer.Application.Contracts.Responses.Promotions;
using AutoBuyer.Domain.Entities;
using AutoBuyer.Domain.Enums;

namespace AutoBuyer.Application.UseCases.Promotions.GetAll;

public sealed class GetAllPromotionCandidatesUseCase
    : IGetAllPromotionCandidatesUseCase
{
    private readonly IPromotionCandidateRepository _repository;

    public GetAllPromotionCandidatesUseCase(
        IPromotionCandidateRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<PromotionCandidateResponse>> ExecuteAsync(
        PromotionCandidateStatus? status,
        CancellationToken cancellationToken)
    {
        var candidates = await _repository.GetAllAsync(
            status,
            cancellationToken);

        return candidates
            .Select(Map)
            .ToList();
    }

    private static PromotionCandidateResponse Map(
        PromotionCandidate candidate)
    {
        return new PromotionCandidateResponse(
            candidate.Id,
            candidate.TelegramChatId,
            candidate.TelegramMessageId,
            candidate.StoreId,
            candidate.Store?.Name,
            candidate.ProductName,
            candidate.AdvertisedPrice,
            candidate.OriginalUrl,
            candidate.ResolvedUrl,
            candidate.Coupon,
            candidate.Conditions,
            candidate.ReviewReason,
            candidate.Status,
            candidate.ProductTargetId,
            candidate.ReceivedAt,
            candidate.ProcessedAt,
            candidate.UpdatedAt);
    }
}
