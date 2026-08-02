using AutoBuyer.Application.Contracts.Responses.Promotions;

namespace AutoBuyer.Application.UseCases.Promotions.ImportMessage;

public sealed record ImportPromotionMessageResult(
    bool Success,
    bool IsDuplicate,
    PromotionCandidateResponse? Promotion,
    string? Error)
{
    public static ImportPromotionMessageResult Imported(
        PromotionCandidateResponse promotion)
    {
        return new ImportPromotionMessageResult(
            Success: true,
            IsDuplicate: false,
            Promotion: promotion,
            Error: null);
    }

    public static ImportPromotionMessageResult Duplicate()
    {
        return new ImportPromotionMessageResult(
            Success: false,
            IsDuplicate: true,
            Promotion: null,
            Error: "Esta mensagem do Telegram já foi importada.");
    }

    public static ImportPromotionMessageResult Failed(
        string error)
    {
        return new ImportPromotionMessageResult(
            Success: false,
            IsDuplicate: false,
            Promotion: null,
            Error: error);
    }
}