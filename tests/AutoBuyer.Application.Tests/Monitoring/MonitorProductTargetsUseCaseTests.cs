using AutoBuyer.Application.Abstractions.Persistence;
using AutoBuyer.Application.Abstractions.Persistence.Models;
using AutoBuyer.Application.Monitoring;
using AutoBuyer.Application.Notifications;
using AutoBuyer.Application.UseCases.Monitoring;
using AutoBuyer.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutoBuyer.Application.Tests.Monitoring;

public sealed class MonitorProductTargetsUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_BlockedStore_DoesNotOpenProductPage()
    {
        var target = new ProductTarget(
            Guid.NewGuid(),
            "Produto monitorado",
            "https://www.lojanova.com.br/produto/123",
            targetPrice: null);
        var priceReader = new ProductPriceReader();
        var storeAccessPolicy = new StoreAccessPolicy(canExecute: false);
        var useCase = new MonitorProductTargetsUseCase(
            new ProductTargetRepository(target),
            new PriceHistoryRepository(),
            priceReader,
            new PriceAlertNotifier(),
            new UnitOfWork(),
            NullLogger<MonitorProductTargetsUseCase>.Instance,
            storeAccessPolicy);

        await useCase.ExecuteAsync(CancellationToken.None);

        Assert.Equal(1, storeAccessPolicy.CanExecuteCount);
        Assert.Equal(0, priceReader.ReadCount);
    }

    private sealed class ProductTargetRepository(
        ProductTarget target) : IProductTargetRepository
    {
        public Task<IReadOnlyList<ProductTarget>>
            GetMonitoringEnabledAsync(
                CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<ProductTarget>>(
                [target]);
        }

        public Task AddAsync(
            ProductTarget productTarget,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ProductTarget?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ProductTarget?> GetTrackedByIdAsync(
            Guid id,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ProductTarget?>
            GetTrackedByStoreAndExternalProductIdAsync(
                Guid storeId,
                string externalProductId,
                CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<ProductTarget>> GetAllAsync(
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<ProductTargetWithLatestPrice>>
            GetAllWithLatestPriceAsync(
                CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ProductTargetWithLatestPrice?>
            GetByIdWithLatestPriceAsync(
                Guid id,
                CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public void Remove(ProductTarget productTarget) =>
            throw new NotSupportedException();
    }

    private sealed class PriceHistoryRepository
        : IPriceHistoryRepository
    {
        public Task AddAsync(
            PriceHistory priceHistory,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<PriceHistory?> GetLatestByProductTargetIdAsync(
            Guid productTargetId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<PriceHistory?>(null);
        }
    }

    private sealed class ProductPriceReader : IProductPriceReader
    {
        public int ReadCount { get; private set; }

        public Task<ProductPriceResult> ReadAsync(
            string productUrl,
            CancellationToken cancellationToken)
        {
            ReadCount++;
            return Task.FromResult(ProductPriceResult.Found(100m));
        }
    }

    private sealed class PriceAlertNotifier : IPriceAlertNotifier
    {
        public Task NotifyAsync(
            PriceAlertNotification notification,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class UnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class StoreAccessPolicy(bool canExecute)
        : IStoreAccessPolicy
    {
        public int CanExecuteCount { get; private set; }

        public Task<bool> CanExecuteAsync(
            Uri productUri,
            CancellationToken cancellationToken)
        {
            CanExecuteCount++;
            return Task.FromResult(canExecute);
        }

        public Task RegisterSuccessAsync(
            Uri productUri,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task RegisterFailureAsync(
            Uri productUri,
            ProductPriceResult result,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
