using AutoBuyer.Application.Abstractions.Persistence;
using AutoBuyer.Application.Abstractions.Persistence.Models;
using AutoBuyer.Application.Contracts.Requests.Promotions;
using AutoBuyer.Application.Promotions.Parsing;
using AutoBuyer.Application.Promotions.Resolution;
using AutoBuyer.Application.UseCases.Promotions.ImportMessage;
using AutoBuyer.Domain.Entities;
using AutoBuyer.Domain.Enums;

namespace AutoBuyer.Application.Tests.Promotions;

public sealed class ImportPromotionMessageUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_SameProductInTwoMessages_UpsertsProductTarget()
    {
        var fixture = new Fixture();

        var first = await fixture.UseCase.ExecuteAsync(
            Request(10, 109m),
            CancellationToken.None);
        var second = await fixture.UseCase.ExecuteAsync(
            Request(11, 99m),
            CancellationToken.None);

        Assert.True(first.Success);
        Assert.True(second.Success);
        Assert.False(second.IsUpdate);
        Assert.Single(fixture.ProductTargets.Items);

        var target = fixture.ProductTargets.Items.Single();

        Assert.Equal("38229", target.ExternalProductId);
        Assert.Equal(99m, target.LastObservedPrice);
        Assert.Null(target.TargetPrice);
        Assert.True(target.MonitoringEnabled);
        Assert.False(target.AutoBuyEnabled);
        Assert.Equal(target.Id, second.Promotion!.ProductTargetId);
    }

    [Fact]
    public async Task ExecuteAsync_EditedTelegramMessage_UpdatesCandidateAndTarget()
    {
        var fixture = new Fixture();

        await fixture.UseCase.ExecuteAsync(
            Request(20, 109m),
            CancellationToken.None);

        var edited = await fixture.UseCase.ExecuteAsync(
            Request(20, 89m) with { IsEdited = true },
            CancellationToken.None);

        Assert.True(edited.Success);
        Assert.True(edited.IsUpdate);
        Assert.Single(fixture.Promotions.Items);
        Assert.Single(fixture.ProductTargets.Items);
        Assert.Equal(89m, edited.Promotion!.AdvertisedPrice);
        Assert.Equal(
            89m,
            fixture.ProductTargets.Items.Single().LastObservedPrice);
        Assert.NotNull(edited.Promotion.UpdatedAt);
    }

    [Fact]
    public async Task ExecuteAsync_UnchangedMessage_ReturnsDuplicateWithoutSaving()
    {
        var fixture = new Fixture();
        var request = Request(30, 109m);

        await fixture.UseCase.ExecuteAsync(
            request,
            CancellationToken.None);
        var saveCount = fixture.UnitOfWork.SaveCount;

        var duplicate = await fixture.UseCase.ExecuteAsync(
            request,
            CancellationToken.None);

        Assert.True(duplicate.IsDuplicate);
        Assert.Equal(saveCount, fixture.UnitOfWork.SaveCount);
        Assert.Single(fixture.Promotions.Items);
        Assert.Single(fixture.ProductTargets.Items);
    }

    private static ImportPromotionMessageRequest Request(
        int messageId,
        decimal price)
    {
        return new ImportPromotionMessageRequest(
            -1001234567890,
            messageId,
            $"""
             🟣 Terabyte
             🔥 Controle Gamer Ninja Sword V2
             ✅ R$ {price:0}
             🔗 https://www.terabyteshop.com.br/produto/38229/controle?p=2212992
             """);
    }

    private sealed class Fixture
    {
        public Fixture()
        {
            Promotions = new PromotionRepository();
            ProductTargets = new ProductTargetRepository();
            Stores = new StoreRepository();
            UnitOfWork = new UnitOfWork();

            UseCase = new ImportPromotionMessageUseCase(
                new TelegramPromotionParser(),
                Promotions,
                ProductTargets,
                Stores,
                new PassthroughUrlResolver(),
                new StoreResolver(),
                new ProductIdentityResolver(),
                UnitOfWork);
        }

        public ImportPromotionMessageUseCase UseCase { get; }

        public PromotionRepository Promotions { get; }

        public ProductTargetRepository ProductTargets { get; }

        public StoreRepository Stores { get; }

        public UnitOfWork UnitOfWork { get; }
    }

    private sealed class PassthroughUrlResolver : IPromotionUrlResolver
    {
        public Task<PromotionUrlResolution> ResolveAsync(
            string originalUrl,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(
                PromotionUrlResolution.Unchanged(originalUrl));
        }
    }

    private sealed class PromotionRepository : IPromotionCandidateRepository
    {
        public List<PromotionCandidate> Items { get; } = [];

        public Task AddAsync(
            PromotionCandidate candidate,
            CancellationToken cancellationToken)
        {
            Items.Add(candidate);
            return Task.CompletedTask;
        }

        public Task<PromotionCandidate?> GetByTelegramSourceAsync(
            long telegramChatId,
            int telegramMessageId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Items.FirstOrDefault(candidate =>
                candidate.TelegramChatId == telegramChatId &&
                candidate.TelegramMessageId == telegramMessageId));
        }

        public Task<PromotionCandidate?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(
                Items.FirstOrDefault(candidate => candidate.Id == id));
        }

        public Task<IReadOnlyList<PromotionCandidate>> GetAllAsync(
            PromotionCandidateStatus? status,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<PromotionCandidate> result = Items
                .Where(candidate =>
                    !status.HasValue || candidate.Status == status)
                .ToArray();

            return Task.FromResult(result);
        }
    }

    private sealed class ProductTargetRepository : IProductTargetRepository
    {
        public List<ProductTarget> Items { get; } = [];

        public Task AddAsync(
            ProductTarget productTarget,
            CancellationToken cancellationToken)
        {
            Items.Add(productTarget);
            return Task.CompletedTask;
        }

        public Task<ProductTarget?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken) =>
            Task.FromResult(Items.FirstOrDefault(item => item.Id == id));

        public Task<ProductTarget?> GetTrackedByIdAsync(
            Guid id,
            CancellationToken cancellationToken) =>
            GetByIdAsync(id, cancellationToken);

        public Task<ProductTarget?> GetTrackedByStoreAndExternalProductIdAsync(
            Guid storeId,
            string externalProductId,
            CancellationToken cancellationToken) =>
            Task.FromResult(Items.FirstOrDefault(item =>
                item.StoreId == storeId &&
                item.ExternalProductId == externalProductId));

        public Task<IReadOnlyList<ProductTarget>> GetAllAsync(
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<ProductTarget>>(Items);

        public Task<IReadOnlyList<ProductTarget>> GetMonitoringEnabledAsync(
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<ProductTarget>>(
                Items.Where(item => item.MonitoringEnabled).ToArray());

        public Task<IReadOnlyList<ProductTargetWithLatestPrice>>
            GetAllWithLatestPriceAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<ProductTargetWithLatestPrice>>([]);

        public Task<ProductTargetWithLatestPrice?> GetByIdWithLatestPriceAsync(
            Guid id,
            CancellationToken cancellationToken) =>
            Task.FromResult<ProductTargetWithLatestPrice?>(null);

        public void Remove(ProductTarget productTarget)
        {
            Items.Remove(productTarget);
        }
    }

    private sealed class StoreRepository : IStoreRepository
    {
        private readonly List<Store> _items = [];

        public Task AddAsync(
            Store store,
            CancellationToken cancellationToken)
        {
            _items.Add(store);
            return Task.CompletedTask;
        }

        public Task<Store?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken) =>
            Task.FromResult(_items.FirstOrDefault(item => item.Id == id));

        public Task<Store?> GetByNameOrBaseUrlAsync(
            string name,
            string baseUrl,
            CancellationToken cancellationToken) =>
            Task.FromResult(_items.FirstOrDefault(item =>
                string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(item.BaseUrl, baseUrl, StringComparison.OrdinalIgnoreCase)));
    }

    private sealed class UnitOfWork : IUnitOfWork
    {
        public int SaveCount { get; private set; }

        public Task<int> SaveChangesAsync(
            CancellationToken cancellationToken)
        {
            SaveCount++;
            return Task.FromResult(1);
        }
    }
}
