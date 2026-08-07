using AutoBuyer.Domain.Entities;

namespace AutoBuyer.Application.Abstractions.Persistence;

public interface IPriceHistoryRepository
{
    Task AddAsync(
        PriceHistory priceHistory,
        CancellationToken cancellationToken);

    Task<PriceHistory?> GetLatestByProductTargetIdAsync(
        Guid productTargetId,
        CancellationToken cancellationToken);
}