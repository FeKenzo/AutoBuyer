using AutoBuyer.Domain.Entities;

namespace AutoBuyer.Application.Abstractions.Persistence;

public interface IStoreRepository
{
    Task<Store?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);
}