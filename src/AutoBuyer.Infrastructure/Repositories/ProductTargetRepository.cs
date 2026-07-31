using AutoBuyer.Application.Abstractions.Persistence;
using AutoBuyer.Domain.Entities;
using AutoBuyer.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AutoBuyer.Infrastructure.Repositories;

public sealed class ProductTargetRepository
    : IProductTargetRepository
{
    private readonly AutoBuyerDbContext _dbContext;

    public ProductTargetRepository(
        AutoBuyerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(
        ProductTarget productTarget,
        CancellationToken cancellationToken)
    {
        await _dbContext.ProductTargets.AddAsync(
            productTarget,
            cancellationToken);
    }

    public Task<ProductTarget?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return _dbContext.ProductTargets
            .AsNoTracking()
            .Include(target => target.Store)
            .FirstOrDefaultAsync(
                target => target.Id == id,
                cancellationToken);
    }

    public async Task<IReadOnlyList<ProductTarget>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        return await _dbContext.ProductTargets
            .AsNoTracking()
            .Include(target => target.Store)
            .OrderByDescending(target => target.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}