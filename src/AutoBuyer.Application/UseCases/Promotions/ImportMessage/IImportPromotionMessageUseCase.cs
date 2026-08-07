using AutoBuyer.Application.Contracts.Requests.Promotions;

namespace AutoBuyer.Application.UseCases.Promotions.ImportMessage;

public interface IImportPromotionMessageUseCase
{
    Task<ImportPromotionMessageResult> ExecuteAsync(
        ImportPromotionMessageRequest request,
        CancellationToken cancellationToken);
}