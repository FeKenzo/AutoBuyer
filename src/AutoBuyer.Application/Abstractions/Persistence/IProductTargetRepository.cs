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
}