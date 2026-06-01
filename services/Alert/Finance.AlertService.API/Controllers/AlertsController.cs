using Finance.AlertService.Application.Common;
using Finance.AlertService.Application.Features.Alerts;
using Finance.AlertService.Application.Features.Alerts.CreateAlert;
using Finance.AlertService.Application.Features.Alerts.DeleteAlert;
using Finance.AlertService.Application.Features.Alerts.GetUserAlerts;
using Finance.AlertService.Infrastructure.Constants;
using Finance.SharedKernel.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Finance.AlertService.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class AlertsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AlertsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<AlertDto>>> GetAlerts(
        [FromQuery] int page = AuthConstants.Pagination.DefaultPage,
        [FromQuery] int pageSize = AuthConstants.Pagination.DefaultPageSize)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var result = await _mediator.Send(new GetUserAlertsQuery(userId, page, pageSize));
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult> CreateAlert([FromBody] CreateAlertCommand command)
    {
        command.UserId = GetUserId();
        if (command.UserId == Guid.Empty) return Unauthorized();

        command.UserEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
            ?? User.FindFirst(AuthConstants.Claims.Email)?.Value
            ?? string.Empty;

        var id = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetAlerts), new { }, new { id });
    }

    [HttpDelete("{alertId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> DeleteAlert(Guid alertId)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        await _mediator.Send(new DeleteAlertCommand(alertId, userId));
        return NoContent();
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirst(AuthConstants.Claims.UserId)?.Value;
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }
}
