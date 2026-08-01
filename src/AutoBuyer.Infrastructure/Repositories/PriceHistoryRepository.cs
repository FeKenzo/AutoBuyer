using AutoBuyer.Application.Abstractions.Persistence;
using AutoBuyer.Domain.Entities;
using AutoBuyer.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AutoBuyer.Infrastructure.Repositories;

public sealed class PriceHistoryRepository
    : IPriceHistoryRepository
{
    private readonly AutoBuyerDbContext _dbContext;

    public PriceHistoryRepository(
        AutoBuyerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(
        PriceHistory priceHistory,
        CancellationToken cancellationToken)
    {
        await _dbContext.PriceHistory.AddAsync(
            priceHistory,
            cancellationToken);
    }

    public Task<PriceHistory?> GetLatestByProductTargetIdAsync(
        Guid productTargetId,
        CancellationToken cancellationToken)
    {
        return _dbContext.PriceHistory
            .AsNoTracking()
            .Where(history =>
                history.ProductTargetId == productTargetId)
            .OrderByDescending(history => history.CapturedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }
}