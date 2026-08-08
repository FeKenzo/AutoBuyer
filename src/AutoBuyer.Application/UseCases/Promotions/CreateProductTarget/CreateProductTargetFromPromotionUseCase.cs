using AutoBuyer.Application.Abstractions.Persistence;
using AutoBuyer.Application.Contracts.Requests.Promotions;
using AutoBuyer.Application.Contracts.Responses.ProductTargets;
using AutoBuyer.Application.Contracts.Responses.Promotions;
using AutoBuyer.Application.Promotions.Resolution;
using AutoBuyer.Domain.Entities;
using AutoBuyer.Domain.Enums;

namespace AutoBuyer.Application.UseCases.Promotions.CreateProductTarget;

public sealed class CreateProductTargetFromPromotionUseCase
    : ICreateProductTargetFromPromotionUseCase
{
    private readonly IPromotionCandidateRepository _promotionRepository;
    private readonly IProductTargetRepository _productTargetRepository;
    private readonly IStoreRepository _storeRepository;
    private readonly IStoreResolver _storeResolver;
    private readonly IProductIdentityResolver _identityResolver;
    private readonly IUnitOfWork _unitOfWork;

    public CreateProductTargetFromPromotionUseCase(
        IPromotionCandidateRepository promotionRepository,
        IProductTargetRepository productTargetRepository,
        IStoreRepository storeRepository,
        IStoreResolver storeResolver,
        IProductIdentityResolver identityResolver,
        IUnitOfWork unitOfWork)
    {
        _promotionRepository = promotionRepository;
        _productTargetRepository = productTargetRepository;
        _storeRepository = storeRepository;
        _storeResolver = storeResolver;
        _identityResolver = identityResolver;
        _unitOfWork = unitOfWork;
    }

    public async Task<CreateProductTargetFromPromotionResult> ExecuteAsync(
        Guid promotionId,
        CreateProductTargetFromPromotionRequest request,
        CancellationToken cancellationToken)
    {
        var promotion = await _promotionRepository.GetByIdAsync(
            promotionId,
            cancellationToken);

        if (promotion is null)
        {
            return CreateProductTargetFromPromotionResult
                .CandidateNotFound();
        }

        if (promotion.Status == PromotionCandidateStatus.Imported ||
            promotion.ProductTargetId.HasValue)
        {
            return CreateProductTargetFromPromotionResult
                .ImportedPreviously();
        }

        var store = await _storeRepository.GetByIdAsync(
            request.StoreId,
            cancellationToken);

        if (store is null)
        {
            return CreateProductTargetFromPromotionResult.Failed(
                "A loja informada não foi encontrada.");
        }

        if (!store.IsEnabled)
        {
            return CreateProductTargetFromPromotionResult.Failed(
                "A loja informada está desabilitada.");
        }

        var productUrl = string.IsNullOrWhiteSpace(request.ProductUrl)
            ? promotion.ResolvedUrl ?? promotion.OriginalUrl
            : request.ProductUrl.Trim();

        ProductTarget productTarget;

        try
        {
            var storeResolution = _storeResolver.Resolve(
                store.Name,
                productUrl);

            if (storeResolution is null ||
                storeResolution.RequiresReview ||
                !string.Equals(
                    storeResolution.Name,
                    store.Name,
                    StringComparison.OrdinalIgnoreCase))
            {
                return CreateProductTargetFromPromotionResult.Failed(
                    "A URL do produto não corresponde à loja selecionada.");
            }

            var identity = _identityResolver.Resolve(
                storeResolution,
                productUrl);

            var existingTarget =
                await _productTargetRepository
                    .GetTrackedByStoreAndExternalProductIdAsync(
                        store.Id,
                        identity.ExternalProductId,
                        cancellationToken);

            productTarget = existingTarget
                ?? new ProductTarget(
                    store.Id,
                    promotion.ProductName,
                    identity.CanonicalUrl,
                    request.TargetPrice,
                    request.AutoBuyEnabled,
                    identity.ExternalProductId,
                    promotion.AdvertisedPrice,
                    monitoringEnabled:
                        storeResolution.SupportsAutomaticMonitoring);

            if (existingTarget is not null)
            {
                productTarget.Observe(
                    promotion.ProductName,
                    identity.CanonicalUrl,
                    promotion.AdvertisedPrice,
                    DateTime.UtcNow);

                if (request.TargetPrice.HasValue)
                {
                    productTarget.ChangeTargetPrice(
                        request.TargetPrice.Value);
                }

                if (request.AutoBuyEnabled)
                    productTarget.EnableAutoBuy();
            }

            if (existingTarget is null)
            {
                await _productTargetRepository.AddAsync(
                    productTarget,
                    cancellationToken);
            }
        }
        catch (ArgumentException exception)
        {
            return CreateProductTargetFromPromotionResult.Failed(
                exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return CreateProductTargetFromPromotionResult.Failed(
                exception.Message);
        }

        promotion.AssignStore(store);
        promotion.SetResolvedUrl(productUrl);
        promotion.MarkAsImported(productTarget);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return CreateProductTargetFromPromotionResult.Created(
            MapProductTarget(productTarget, store.Name),
            MapPromotion(promotion));
    }

    private static ProductTargetResponse MapProductTarget(
        ProductTarget productTarget,
        string storeName)
    {
        return new ProductTargetResponse(
            productTarget.Id,
            productTarget.StoreId,
            storeName,
            productTarget.Name,
            productTarget.ProductUrl,
            productTarget.ExternalProductId,
            productTarget.TargetPrice,
            productTarget.LastObservedPrice,
            productTarget.LastSeenAt,
            CurrentPrice: null,
            TargetReached: false,
            LastCapturedAt: null,
            productTarget.AutoBuyEnabled,
            productTarget.MonitoringEnabled,
            productTarget.CreatedAt,
            productTarget.UpdatedAt);
    }

    private static PromotionCandidateResponse MapPromotion(
        PromotionCandidate promotion)
    {
        return new PromotionCandidateResponse(
            promotion.Id,
            promotion.TelegramChatId,
            promotion.TelegramMessageId,
            promotion.StoreId,
            promotion.Store?.Name,
            promotion.ProductName,
            promotion.AdvertisedPrice,
            promotion.OriginalUrl,
            promotion.ResolvedUrl,
            promotion.Coupon,
            promotion.Conditions,
            promotion.ReviewReason,
            promotion.Status,
            promotion.ProductTargetId,
            promotion.ReceivedAt,
            promotion.ProcessedAt,
            promotion.UpdatedAt);
    }
}
