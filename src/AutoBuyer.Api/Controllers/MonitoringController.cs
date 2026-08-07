using AutoBuyer.Application.Abstractions.Persistence;
using AutoBuyer.Application.Contracts.Responses.Monitoring;
using Microsoft.AspNetCore.Mvc;

namespace AutoBuyer.Api.Controllers;

[ApiController]
[Route("api/monitoring")]
public sealed class MonitoringController : ControllerBase
{
    [HttpGet("stores")]
    public async Task<IActionResult> GetStoreStates(
        [FromServices]
        IStoreMonitoringStateRepository repository,
        CancellationToken cancellationToken)
    {
        var states = await repository.GetAllAsync(
            cancellationToken);

        var response = states.Select(state =>
            new StoreMonitoringStateResponse(
                state.Host,
                state.Status,
                state.ConsecutiveFailures,
                state.LastHttpStatusCode,
                state.LastError,
                state.LastSuccessAt,
                state.LastFailureAt,
                state.NextAllowedAttemptAt,
                state.UpdatedAt));

        return Ok(response);
    }
}