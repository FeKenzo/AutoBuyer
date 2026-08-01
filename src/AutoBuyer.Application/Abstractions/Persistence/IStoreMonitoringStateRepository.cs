using AutoBuyer.Domain.Entities;

namespace AutoBuyer.Application.Abstractions.Persistence;

public interface IStoreMonitoringStateRepository
{
    Task<StoreMonitoringState?> GetByHostAsync(
        string host,
        CancellationToken cancellationToken);

    Task AddAsync(
        StoreMonitoringState state,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<StoreMonitoringState>> GetAllAsync(
        CancellationToken cancellationToken);
}