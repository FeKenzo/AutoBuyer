using AutoBuyer.Application.Abstractions.Persistence;
using AutoBuyer.Application.Monitoring;
using AutoBuyer.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutoBuyer.Application.Tests.Monitoring;

public sealed class StoreAccessPolicyTests
{
    [Fact]
    public async Task CanExecuteAsync_MissingState_AllowsFirstAttempt()
    {
        var repository = new StoreMonitoringStateRepository();
        var policy = new StoreAccessPolicy(
            repository,
            new UnitOfWork(),
            NullLogger<StoreAccessPolicy>.Instance);

        var canExecute = await policy.CanExecuteAsync(
            new Uri("https://www.lojanova.com.br/produto/123"),
            CancellationToken.None);

        Assert.True(canExecute);
        Assert.Empty(repository.Items);
    }

    private sealed class StoreMonitoringStateRepository
        : IStoreMonitoringStateRepository
    {
        public List<StoreMonitoringState> Items { get; } = [];

        public Task<StoreMonitoringState?> GetByHostAsync(
            string host,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(
                Items.FirstOrDefault(state => state.Host == host));
        }

        public Task AddAsync(
            StoreMonitoringState state,
            CancellationToken cancellationToken)
        {
            Items.Add(state);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<StoreMonitoringState>> GetAllAsync(
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<StoreMonitoringState>>(
                Items);
        }
    }

    private sealed class UnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(
            CancellationToken cancellationToken)
        {
            return Task.FromResult(0);
        }
    }
}
