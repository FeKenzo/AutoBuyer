using AutoBuyer.Domain.Entities;

namespace AutoBuyer.Application.Abstractions.Persistence;

public interface IStoreRepository
{
    Task AddAsync(
        Store store,
        CancellationToken cancellationToken);

    Task<Store?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<Store?> GetByNameOrBaseUrlAsync(
        string name,
        string baseUrl,
        CancellationToken cancellationToken);
}
