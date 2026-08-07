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

    public async Task AddAsync(
        Store store,
        CancellationToken cancellationToken)
    {
        await _dbContext.Stores.AddAsync(
            store,
            cancellationToken);
    }

    public Task<Store?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return _dbContext.Stores
            .FirstOrDefaultAsync(
                store => store.Id == id,
                cancellationToken);
    }

    public Task<Store?> GetByNameOrBaseUrlAsync(
        string name,
        string baseUrl,
        CancellationToken cancellationToken)
    {
        return _dbContext.Stores
            .FirstOrDefaultAsync(
                store =>
                    store.Name == name ||
                    store.BaseUrl == baseUrl,
                cancellationToken);
    }
}
