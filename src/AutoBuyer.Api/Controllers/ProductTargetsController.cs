using AutoBuyer.Application.Contracts.Requests.ProductTargets;
using AutoBuyer.Application.Contracts.Responses.ProductTargets;
using AutoBuyer.Application.UseCases.ProductTargets.Create;
using AutoBuyer.Application.UseCases.ProductTargets.GetAll;
using AutoBuyer.Application.UseCases.ProductTargets.GetById;
using Microsoft.AspNetCore.Mvc;
using AutoBuyer.Application.UseCases.ProductTargets.ChangeMonitoringStatus;
using AutoBuyer.Application.UseCases.ProductTargets.Delete;
using AutoBuyer.Application.UseCases.ProductTargets.Update;

namespace AutoBuyer.Api.Controllers;

[ApiController]
[Route("api/product-targets")]
public sealed class ProductTargetsController : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(
        typeof(ProductTargetResponse),
        StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateProductTargetRequest request,
        [FromServices] ICreateProductTargetUseCase useCase,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await useCase.ExecuteAsync(
                request,
                cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new { id = result.Id },
                result);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new
            {
                error = exception.Message
            });
        }
        catch (KeyNotFoundException exception)
        {
            return BadRequest(new
            {
                error = exception.Message
            });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new
            {
                error = exception.Message
            });
        }
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(IReadOnlyList<ProductTargetResponse>),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromServices] IGetAllProductTargetsUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(
        typeof(ProductTargetResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        Guid id,
        [FromServices] IGetProductTargetByIdUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(
            id,
            cancellationToken);

        return result is null
            ? NotFound()
            : Ok(result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(
    typeof(ProductTargetResponse),
    StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
    Guid id,
    [FromBody] UpdateProductTargetRequest request,
    [FromServices] IUpdateProductTargetUseCase useCase,
    CancellationToken cancellationToken)
    {
        try
        {
            var result = await useCase.ExecuteAsync(
                id,
                request,
                cancellationToken);

            return result is null
                ? NotFound()
                : Ok(result);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new
            {
                error = exception.Message
            });
        }
    }

    [HttpPatch("{id:guid}/monitoring")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangeMonitoringStatus(
        Guid id,
        [FromBody] ChangeMonitoringStatusRequest request,
        [FromServices] IChangeMonitoringStatusUseCase useCase,
        CancellationToken cancellationToken)
    {
        var updated = await useCase.ExecuteAsync(
            id,
            request.Enabled,
            cancellationToken);

        return updated
            ? NoContent()
            : NotFound();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromServices] IDeleteProductTargetUseCase useCase,
        CancellationToken cancellationToken)
    {
        var deleted = await useCase.ExecuteAsync(
            id,
            cancellationToken);

        return deleted
            ? NoContent()
            : NotFound();
    }
}