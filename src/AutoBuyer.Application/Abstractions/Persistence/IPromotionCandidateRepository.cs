using AutoBuyer.Domain.Entities;
using AutoBuyer.Domain.Enums;

namespace AutoBuyer.Application.Abstractions.Persistence;

public interface IPromotionCandidateRepository
{
    Task AddAsync(
        PromotionCandidate candidate,
        CancellationToken cancellationToken);

    Task<PromotionCandidate?> GetByTelegramSourceAsync(
        long telegramChatId,
        int telegramMessageId,
        CancellationToken cancellationToken);

    Task<PromotionCandidate?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<PromotionCandidate>> GetAllAsync(
        PromotionCandidateStatus? status,
        CancellationToken cancellationToken);
}
