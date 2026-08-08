using AutoBuyer.Application.Abstractions.Persistence;
using AutoBuyer.Domain.Entities;
using AutoBuyer.Domain.Enums;
using AutoBuyer.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AutoBuyer.Infrastructure.Repositories;

public sealed class PromotionCandidateRepository
    : IPromotionCandidateRepository
{
    private readonly AutoBuyerDbContext _dbContext;

    public PromotionCandidateRepository(
        AutoBuyerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(
        PromotionCandidate candidate,
        CancellationToken cancellationToken)
    {
        await _dbContext.PromotionCandidates.AddAsync(
            candidate,
            cancellationToken);
    }

    public Task<PromotionCandidate?> GetByTelegramSourceAsync(
        long telegramChatId,
        int telegramMessageId,
        CancellationToken cancellationToken)
    {
        return _dbContext.PromotionCandidates
            .Include(candidate => candidate.Store)
            .Include(candidate => candidate.ProductTarget)
            .FirstOrDefaultAsync(
                candidate =>
                    candidate.TelegramChatId == telegramChatId
                    && candidate.TelegramMessageId
                        == telegramMessageId,
                cancellationToken);
    }

    public Task<PromotionCandidate?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return _dbContext.PromotionCandidates
            .Include(candidate => candidate.Store)
            .Include(candidate => candidate.ProductTarget)
            .FirstOrDefaultAsync(
                candidate => candidate.Id == id,
                cancellationToken);
    }

    public async Task<IReadOnlyList<PromotionCandidate>> GetAllAsync(
        PromotionCandidateStatus? status,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.PromotionCandidates
            .AsNoTracking()
            .Include(candidate => candidate.Store)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(
                candidate => candidate.Status == status.Value);
        }

        return await query
            .OrderByDescending(candidate => candidate.ReceivedAt)
            .ToListAsync(cancellationToken);
    }
}
