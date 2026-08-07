using AutoBuyer.Application.Contracts.Responses.ProductTargets;
using AutoBuyer.Application.Contracts.Responses.Promotions;

namespace AutoBuyer.Application.UseCases.Promotions.CreateProductTarget;

public sealed record CreateProductTargetFromPromotionResult(
    bool Success,
    bool NotFound,
    bool AlreadyImported,
    ProductTargetResponse? ProductTarget,
    PromotionCandidateResponse? Promotion,
    string? Error)
{
    public static CreateProductTargetFromPromotionResult Created(
        ProductTargetResponse productTarget,
        PromotionCandidateResponse promotion)
    {
        return new(
            Success: true,
            NotFound: false,
            AlreadyImported: false,
            ProductTarget: productTarget,
            Promotion: promotion,
            Error: null);
    }

    public static CreateProductTargetFromPromotionResult CandidateNotFound()
    {
        return new(
            false,
            true,
            false,
            null,
            null,
            "A promoção não foi encontrada.");
    }

    public static CreateProductTargetFromPromotionResult ImportedPreviously()
    {
        return new(
            false,
            false,
            true,
            null,
            null,
            "A promoção já foi convertida em monitoramento.");
    }

    public static CreateProductTargetFromPromotionResult Failed(string error)
    {
        return new(
            false,
            false,
            false,
            null,
            null,
            error);
    }
}