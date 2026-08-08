using AutoBuyer.Application.Contracts.Responses.Promotions;

namespace AutoBuyer.Application.UseCases.Promotions.ImportMessage;

public sealed record ImportPromotionMessageResult(
    bool Success,
    bool IsDuplicate,
    bool IsUpdate,
    PromotionCandidateResponse? Promotion,
    string? Error)
{
    public static ImportPromotionMessageResult Imported(
        PromotionCandidateResponse promotion,
        bool isUpdate)
    {
        return new ImportPromotionMessageResult(
            Success: true,
            IsDuplicate: false,
            IsUpdate: isUpdate,
            Promotion: promotion,
            Error: null);
    }

    public static ImportPromotionMessageResult Duplicate()
    {
        return new ImportPromotionMessageResult(
            Success: false,
            IsDuplicate: true,
            IsUpdate: false,
            Promotion: null,
            Error: "Esta mensagem do Telegram já foi importada.");
    }

    public static ImportPromotionMessageResult Failed(
        string error)
    {
        return new ImportPromotionMessageResult(
            Success: false,
            IsDuplicate: false,
            IsUpdate: false,
            Promotion: null,
            Error: error);
    }
}
