using AutoBuyer.Application.Contracts.Requests.Promotions;
using AutoBuyer.Application.Contracts.Responses.Promotions;
using AutoBuyer.Application.UseCases.Promotions.ImportMessage;
using Microsoft.AspNetCore.Mvc;

namespace AutoBuyer.Api.Controllers;

[ApiController]
[Route("api/promotions")]
public sealed class PromotionsController : ControllerBase
{
    [HttpPost("import-message")]
    [ProducesResponseType(
        typeof(PromotionCandidateResponse),
        StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ImportMessage(
        [FromBody] ImportPromotionMessageRequest request,
        [FromServices] IImportPromotionMessageUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(
            request,
            cancellationToken);

        if (result.IsDuplicate)
        {
            return Conflict(new
            {
                error = result.Error
            });
        }

        if (!result.Success || result.Promotion is null)
        {
            return BadRequest(new
            {
                error = result.Error
                    ?? "Não foi possível importar a promoção."
            });
        }

        return StatusCode(
            StatusCodes.Status201Created,
            result.Promotion);
    }
}