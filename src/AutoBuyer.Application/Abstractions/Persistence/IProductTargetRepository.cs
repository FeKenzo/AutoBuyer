using AutoBuyer.Application.Abstractions.Persistence.Models;
using AutoBuyer.Domain.Entities;

namespace AutoBuyer.Application.Abstractions.Persistence;

public interface IProductTargetRepository
{
    Task AddAsync(
        ProductTarget productTarget,
        CancellationToken cancellationToken);

    Task<ProductTarget?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ProductTarget>> GetAllAsync(
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ProductTarget>>
        GetMonitoringEnabledAsync(
            CancellationToken cancellationToken);

    Task<IReadOnlyList<ProductTargetWithLatestPrice>>
        GetAllWithLatestPriceAsync(
            CancellationToken cancellationToken);

    Task<ProductTargetWithLatestPrice?>
        GetByIdWithLatestPriceAsync(
            Guid id,
            CancellationToken cancellationToken);
}