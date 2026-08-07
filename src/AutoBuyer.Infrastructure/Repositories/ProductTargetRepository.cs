using AutoBuyer.Application.Abstractions.Persistence;
using AutoBuyer.Domain.Entities;
using AutoBuyer.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using AutoBuyer.Application.Abstractions.Persistence.Models;

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
    public async Task<IReadOnlyList<ProductTarget>>
    GetMonitoringEnabledAsync(
        CancellationToken cancellationToken)
    {
        return await _dbContext.ProductTargets
            .AsNoTracking()
            .Where(target => target.MonitoringEnabled)
            .Where(target => target.TargetPrice.HasValue)
            .Where(target =>
                target.Store != null &&
                target.Store.IsEnabled)
            .Include(target => target.Store)
            .OrderBy(target => target.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProductTargetWithLatestPrice>>
    GetAllWithLatestPriceAsync(
        CancellationToken cancellationToken)
    {
        return await _dbContext.ProductTargets
            .AsNoTracking()
            .OrderByDescending(target => target.CreatedAt)
            .Select(target => new ProductTargetWithLatestPrice(
                target.Id,
                target.StoreId,
                target.Store != null
                    ? target.Store.Name
                    : string.Empty,
                target.Name,
                target.ProductUrl,
                target.ExternalProductId,
                target.TargetPrice,
                target.LastObservedPrice,
                target.LastSeenAt,
                target.AutoBuyEnabled,
                target.MonitoringEnabled,
                target.CreatedAt,
                target.UpdatedAt,
                _dbContext.PriceHistory
                    .Where(history =>
                        history.ProductTargetId == target.Id)
                    .OrderByDescending(history => history.CapturedAt)
                    .Select(history => (decimal?)history.Price)
                    .FirstOrDefault(),
                _dbContext.PriceHistory
                    .Where(history =>
                        history.ProductTargetId == target.Id)
                    .OrderByDescending(history => history.CapturedAt)
                    .Select(history => (DateTime?)history.CapturedAt)
                    .FirstOrDefault()))
            .ToListAsync(cancellationToken);
    }

    public Task<ProductTargetWithLatestPrice?>
        GetByIdWithLatestPriceAsync(
            Guid id,
            CancellationToken cancellationToken)
    {
        return _dbContext.ProductTargets
            .AsNoTracking()
            .Where(target => target.Id == id)
            .Select(target => new ProductTargetWithLatestPrice(
                target.Id,
                target.StoreId,
                target.Store != null
                    ? target.Store.Name
                    : string.Empty,
                target.Name,
                target.ProductUrl,
                target.ExternalProductId,
                target.TargetPrice,
                target.LastObservedPrice,
                target.LastSeenAt,
                target.AutoBuyEnabled,
                target.MonitoringEnabled,
                target.CreatedAt,
                target.UpdatedAt,
                _dbContext.PriceHistory
                    .Where(history =>
                        history.ProductTargetId == target.Id)
                    .OrderByDescending(history => history.CapturedAt)
                    .Select(history => (decimal?)history.Price)
                    .FirstOrDefault(),
                _dbContext.PriceHistory
                    .Where(history =>
                        history.ProductTargetId == target.Id)
                    .OrderByDescending(history => history.CapturedAt)
                    .Select(history => (DateTime?)history.CapturedAt)
                    .FirstOrDefault()))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<ProductTarget?> GetTrackedByIdAsync(
    Guid id,
    CancellationToken cancellationToken)
    {
        return _dbContext.ProductTargets
            .FirstOrDefaultAsync(
                target => target.Id == id,
                cancellationToken);
    }

    public Task<ProductTarget?>
        GetTrackedByStoreAndExternalProductIdAsync(
            Guid storeId,
            string externalProductId,
            CancellationToken cancellationToken)
    {
        return _dbContext.ProductTargets
            .FirstOrDefaultAsync(
                target =>
                    target.StoreId == storeId &&
                    target.ExternalProductId == externalProductId,
                cancellationToken);
    }

    public void Remove(ProductTarget productTarget)
    {
        _dbContext.ProductTargets.Remove(productTarget);
    }
}
