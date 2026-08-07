using AutoBuyer.Application.Abstractions.Persistence;
using AutoBuyer.Application.Contracts.Requests.Promotions;
using AutoBuyer.Application.Contracts.Responses.Promotions;
using AutoBuyer.Application.Promotions.Parsing;
using AutoBuyer.Domain.Entities;

namespace AutoBuyer.Application.UseCases.Promotions.ImportMessage;

public sealed class ImportPromotionMessageUseCase
    : IImportPromotionMessageUseCase
{
    private readonly IPromotionMessageParser _parser;
    private readonly IPromotionCandidateRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public ImportPromotionMessageUseCase(
        IPromotionMessageParser parser,
        IPromotionCandidateRepository repository,
        IUnitOfWork unitOfWork)
    {
        _parser = parser;
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ImportPromotionMessageResult> ExecuteAsync(
        ImportPromotionMessageRequest request,
        CancellationToken cancellationToken)
    {
        if (request.TelegramChatId == 0)
        {
            return ImportPromotionMessageResult.Failed(
                "O identificador do chat do Telegram é obrigatório.");
        }

        if (request.TelegramMessageId <= 0)
        {
            return ImportPromotionMessageResult.Failed(
                "O identificador da mensagem deve ser maior que zero.");
        }

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return ImportPromotionMessageResult.Failed(
                "A mensagem da promoção é obrigatória.");
        }

        var alreadyExists = await _repository.ExistsAsync(
            request.TelegramChatId,
            request.TelegramMessageId,
            cancellationToken);

        if (alreadyExists)
        {
            return ImportPromotionMessageResult.Duplicate();
        }

        var parseResult = _parser.Parse(request.Message);

        if (!parseResult.Success)
        {
            return ImportPromotionMessageResult.Failed(
                parseResult.Error
                ?? "Não foi possível interpretar a mensagem.");
        }

        if (string.IsNullOrWhiteSpace(parseResult.ProductName)
            || !parseResult.AdvertisedPrice.HasValue
            || string.IsNullOrWhiteSpace(parseResult.Url))
        {
            return ImportPromotionMessageResult.Failed(
                "O parser não retornou todos os dados obrigatórios.");
        }

        var candidate = new PromotionCandidate(
            request.TelegramChatId,
            request.TelegramMessageId,
            parseResult.ProductName,
            parseResult.AdvertisedPrice.Value,
            parseResult.Url,
            request.Message,
            parseResult.Coupon,
            parseResult.Conditions);

        /*
         * Promoções com cupom ou condições adicionais ainda não
         * devem virar ProductTarget automaticamente.
         */
        if (!string.IsNullOrWhiteSpace(parseResult.Coupon)
            || !string.IsNullOrWhiteSpace(parseResult.Conditions))
        {
            candidate.MarkAsNeedsReview();
        }

        await _repository.AddAsync(
            candidate,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return ImportPromotionMessageResult.Imported(
            Map(candidate));
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
            candidate.Status,
            candidate.ProductTargetId,
            candidate.ReceivedAt,
            candidate.ProcessedAt);
    }
}