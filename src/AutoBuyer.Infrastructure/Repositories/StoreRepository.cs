using AutoBuyer.Application.Abstractions.Persistence;
using AutoBuyer.Domain.Entities;
using AutoBuyer.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AutoBuyer.Infrastructure.Repositories;

public sealed class StoreRepository : IStoreRepository
{
    private readonly AutoBuyerDbContext _dbContext;

    public StoreRepository(AutoBuyerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Store?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return _dbContext.Stores
            .AsNoTracking()
            .FirstOrDefaultAsync(
                store => store.Id == id,
                cancellationToken);
    }
}