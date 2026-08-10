using AutoBuyer.Application.Abstractions.Persistence;
using AutoBuyer.Application.Contracts.Requests.Promotions;
using AutoBuyer.Application.Contracts.Responses.Promotions;
using AutoBuyer.Application.Promotions.Parsing;
using AutoBuyer.Application.Promotions.Resolution;
using AutoBuyer.Domain.Entities;
using AutoBuyer.Domain.Enums;

namespace AutoBuyer.Application.UseCases.Promotions.ImportMessage;

public sealed class ImportPromotionMessageUseCase
    : IImportPromotionMessageUseCase
{
    private readonly IPromotionMessageParser _parser;
    private readonly IPromotionCandidateRepository _promotionRepository;
    private readonly IProductTargetRepository _productTargetRepository;
    private readonly IStoreRepository _storeRepository;
    private readonly IStoreResolver _storeResolver;
    private readonly IProductIdentityResolver _identityResolver;
    private readonly IUnitOfWork _unitOfWork;

    public ImportPromotionMessageUseCase(
        IPromotionMessageParser parser,
        IPromotionCandidateRepository promotionRepository,
        IProductTargetRepository productTargetRepository,
        IStoreRepository storeRepository,
        IStoreResolver storeResolver,
        IProductIdentityResolver identityResolver,
        IUnitOfWork unitOfWork)
    {
        _parser = parser;
        _promotionRepository = promotionRepository;
        _productTargetRepository = productTargetRepository;
        _storeRepository = storeRepository;
        _storeResolver = storeResolver;
        _identityResolver = identityResolver;
        _unitOfWork = unitOfWork;
    }

    public async Task<ImportPromotionMessageResult> ExecuteAsync(
        ImportPromotionMessageRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = Validate(request);

        if (validationError is not null)
            return ImportPromotionMessageResult.Failed(validationError);

        var existingCandidate =
            await _promotionRepository.GetByTelegramSourceAsync(
                request.TelegramChatId,
                request.TelegramMessageId,
                cancellationToken);

        if (existingCandidate is not null &&
            existingCandidate.OriginalMessage == request.Message.Trim() &&
            existingCandidate.Status is
                PromotionCandidateStatus.Imported or
                PromotionCandidateStatus.Ignored)
        {
            return ImportPromotionMessageResult.Duplicate();
        }

        var parseResult = _parser.Parse(request.Message);

        if (!parseResult.Success ||
            string.IsNullOrWhiteSpace(parseResult.ProductName) ||
            !parseResult.AdvertisedPrice.HasValue ||
            string.IsNullOrWhiteSpace(parseResult.Url))
        {
            return ImportPromotionMessageResult.Failed(
                parseResult.Error ??
                "O parser não retornou todos os dados obrigatórios.");
        }

        PromotionCandidate candidate;

        try
        {
            candidate = existingCandidate ?? new PromotionCandidate(
                request.TelegramChatId,
                request.TelegramMessageId,
                parseResult.ProductName,
                parseResult.AdvertisedPrice.Value,
                parseResult.Url,
                request.Message,
                parseResult.Coupon,
                parseResult.Conditions);

            if (existingCandidate is not null)
            {
                candidate.UpdateFromTelegramMessage(
                    parseResult.ProductName,
                    parseResult.AdvertisedPrice.Value,
                    parseResult.Url,
                    request.Message,
                    parseResult.Coupon,
                    parseResult.Conditions);
            }
        }
        catch (ArgumentException exception)
        {
            return ImportPromotionMessageResult.Failed(
                exception.Message);
        }

        if (existingCandidate is null)
        {
            await _promotionRepository.AddAsync(
                candidate,
                cancellationToken);
        }

        var storeResolution = _storeResolver.Resolve(
            parseResult.StoreName,
            parseResult.Url);

        if (storeResolution is null)
        {
            candidate.MarkAsUnsupportedStore(
                "Não foi possível identificar a loja da promoção.");

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return ImportPromotionMessageResult.Imported(
                Map(candidate),
                existingCandidate is not null);
        }

        var store = await GetOrCreateStoreAsync(
            storeResolution,
            cancellationToken);

        candidate.AssignStore(store);

        if (!store.IsEnabled ||
            parseResult.NeedsReview ||
            storeResolution.RequiresReview)
        {
            candidate.MarkAsNeedsReview(
                !store.IsEnabled
                    ? "A loja identificada está desabilitada."
                    : storeResolution.RequiresReview
                        ? "A loja informada na mensagem diverge do domínio do link."
                        : "O preço da mensagem é ambíguo.");

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return ImportPromotionMessageResult.Imported(
                Map(candidate),
                existingCandidate is not null);
        }

        var identity = _identityResolver.Resolve(
            storeResolution,
            parseResult.Url);

        var productTarget =
            await _productTargetRepository
                .GetTrackedByStoreAndExternalProductIdAsync(
                    store.Id,
                    identity.ExternalProductId,
                    cancellationToken);

        var observedAt = DateTime.UtcNow;

        if (productTarget is null)
        {
            productTarget = new ProductTarget(
                store.Id,
                parseResult.ProductName,
                identity.CanonicalUrl,
                targetPrice: null,
                autoBuyEnabled: false,
                externalProductId: identity.ExternalProductId,
                lastObservedPrice: parseResult.AdvertisedPrice.Value,
                monitoringEnabled: true);

            await _productTargetRepository.AddAsync(
                productTarget,
                cancellationToken);
        }
        else
        {
            productTarget.Observe(
                parseResult.ProductName,
                identity.CanonicalUrl,
                parseResult.AdvertisedPrice.Value,
                observedAt);
        }

        candidate.MarkAsImported(productTarget);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ImportPromotionMessageResult.Imported(
            Map(candidate),
            existingCandidate is not null);
    }

    private async Task<Store> GetOrCreateStoreAsync(
        StoreResolution resolution,
        CancellationToken cancellationToken)
    {
        var store = await _storeRepository.GetByNameOrBaseUrlAsync(
            resolution.Name,
            resolution.BaseUrl,
            cancellationToken);

        if (store is not null)
            return store;

        store = new Store(
            resolution.Name,
            resolution.BaseUrl);

        await _storeRepository.AddAsync(
            store,
            cancellationToken);

        return store;
    }

    private static string? Validate(
        ImportPromotionMessageRequest request)
    {
        if (request.TelegramChatId == 0)
            return "O identificador do chat do Telegram é obrigatório.";

        if (request.TelegramMessageId <= 0)
        {
            return "O identificador da mensagem deve ser maior que zero.";
        }

        return string.IsNullOrWhiteSpace(request.Message)
            ? "A mensagem da promoção é obrigatória."
            : null;
    }

    private static PromotionCandidateResponse Map(
        PromotionCandidate candidate)
    {
        return new PromotionCandidateResponse(
            candidate.Id,
            candidate.TelegramChatId,
            candidate.TelegramMessageId,
            candidate.StoreId,
            candidate.Store?.Name,
            candidate.ProductName,
            candidate.AdvertisedPrice,
            candidate.OriginalUrl,
            candidate.ResolvedUrl,
            candidate.Coupon,
            candidate.Conditions,
            candidate.ReviewReason,
            candidate.Status,
            candidate.ProductTargetId,
            candidate.ReceivedAt,
            candidate.ProcessedAt,
            candidate.UpdatedAt);
    }
}
