using AutoBuyer.Application.Abstractions.Persistence;
using AutoBuyer.Application.Contracts.Requests.Promotions;
using AutoBuyer.Application.Contracts.Responses.ProductTargets;
using AutoBuyer.Application.Contracts.Responses.Promotions;
using AutoBuyer.Domain.Entities;
using AutoBuyer.Domain.Enums;

namespace AutoBuyer.Application.UseCases.Promotions.CreateProductTarget;

public sealed class CreateProductTargetFromPromotionUseCase
    : ICreateProductTargetFromPromotionUseCase
{
    private readonly IPromotionCandidateRepository _promotionRepository;
    private readonly IProductTargetRepository _productTargetRepository;
    private readonly IStoreRepository _storeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateProductTargetFromPromotionUseCase(
        IPromotionCandidateRepository promotionRepository,
        IProductTargetRepository productTargetRepository,
        IStoreRepository storeRepository,
        IUnitOfWork unitOfWork)
    {
        _promotionRepository = promotionRepository;
        _productTargetRepository = productTargetRepository;
        _storeRepository = storeRepository;
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

        if (promotion.Status == PromotionCandidateStatus.Imported
            || promotion.ProductTargetId.HasValue)
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

        var targetPrice =
            request.TargetPrice ?? promotion.AdvertisedPrice;

        ProductTarget productTarget;

        try
        {
            productTarget = new ProductTarget(
                store.Id,
                promotion.ProductName,
                productUrl,
                targetPrice,
                request.AutoBuyEnabled);
        }
        catch (ArgumentException exception)
        {
            return CreateProductTargetFromPromotionResult.Failed(
                exception.Message);
        }

        promotion.AssignStore(store);
        promotion.MarkAsImported(productTarget);

        await _productTargetRepository.AddAsync(
            productTarget,
            cancellationToken);

        /*
         * PromotionCandidate e ProductTarget são persistidos
         * na mesma transação do DbContext.
         */
        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

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
            productTarget.TargetPrice,
            CurrentPrice: null,
            TargetReached: false,
            LastCapturedAt: null,
            productTarget.AutoBuyEnabled,
            productTarget.MonitoringEnabled,
            productTarget.CreatedAt);
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
            promotion.Status,
            promotion.ProductTargetId,
            promotion.ReceivedAt,
            promotion.ProcessedAt);
    }
}