using AutoBuyer.Application.Abstractions.Persistence;
using AutoBuyer.Domain.Entities;
using AutoBuyer.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AutoBuyer.Infrastructure.Repositories;

public sealed class StoreMonitoringStateRepository
    : IStoreMonitoringStateRepository
{
    private readonly AutoBuyerDbContext _dbContext;

    public StoreMonitoringStateRepository(
        AutoBuyerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<StoreMonitoringState?> GetByHostAsync(
        string host,
        CancellationToken cancellationToken)
    {
        var normalizedHost = host.Trim().ToLowerInvariant();

        return _dbContext.StoreMonitoringStates
            .FirstOrDefaultAsync(
                state => state.Host == normalizedHost,
                cancellationToken);
    }

    public async Task AddAsync(
        StoreMonitoringState state,
        CancellationToken cancellationToken)
    {
        await _dbContext.StoreMonitoringStates.AddAsync(
            state,
            cancellationToken);
    }

    public async Task<IReadOnlyList<StoreMonitoringState>> GetAllAsync(
    CancellationToken cancellationToken)
    {
        return await _dbContext.StoreMonitoringStates
            .AsNoTracking()
            .OrderBy(state => state.Host)
            .ToListAsync(cancellationToken);
    }
}