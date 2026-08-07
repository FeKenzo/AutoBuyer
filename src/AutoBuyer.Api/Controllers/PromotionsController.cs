using AutoBuyer.Application.Contracts.Requests.Promotions;
using AutoBuyer.Application.Contracts.Responses.Promotions;
using AutoBuyer.Application.UseCases.Promotions.CreateProductTarget;
using AutoBuyer.Application.UseCases.Promotions.GetAll;
using AutoBuyer.Application.UseCases.Promotions.Ignore;
using AutoBuyer.Application.UseCases.Promotions.ImportMessage;
using AutoBuyer.Domain.Enums;
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
    [ProducesResponseType(
        typeof(PromotionCandidateResponse),
        StatusCodes.Status200OK)]
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
            return Conflict(new { error = result.Error });
        }

        if (!result.Success || result.Promotion is null)
        {
            return BadRequest(new
            {
                error = result.Error
                    ?? "Não foi possível importar a promoção."
            });
        }

        return result.IsUpdate
            ? Ok(result.Promotion)
            : StatusCode(
                StatusCodes.Status201Created,
                result.Promotion);
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(IReadOnlyList<PromotionCandidateResponse>),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] PromotionCandidateStatus? status,
        [FromServices] IGetAllPromotionCandidatesUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(
            status,
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("{id:guid}/product-target")]
    [ProducesResponseType(
        typeof(CreateProductTargetFromPromotionResult),
        StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateProductTarget(
        Guid id,
        [FromBody] CreateProductTargetFromPromotionRequest request,
        [FromServices]
        ICreateProductTargetFromPromotionUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(
            id,
            request,
            cancellationToken);

        if (result.NotFound)
        {
            return NotFound(new { error = result.Error });
        }

        if (result.AlreadyImported)
        {
            return Conflict(new { error = result.Error });
        }

        if (!result.Success)
        {
            return BadRequest(new { error = result.Error });
        }

        return StatusCode(
            StatusCodes.Status201Created,
            result);
    }

    [HttpPatch("{id:guid}/ignore")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Ignore(
        Guid id,
        [FromServices] IIgnorePromotionCandidateUseCase useCase,
        CancellationToken cancellationToken)
    {
        try
        {
            var ignored = await useCase.ExecuteAsync(
                id,
                cancellationToken);

            return ignored
                ? NoContent()
                : NotFound();
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new
            {
                error = exception.Message
            });
        }
    }
}
