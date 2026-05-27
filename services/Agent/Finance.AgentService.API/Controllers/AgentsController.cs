using Finance.AgentService.Infrastructure.Orchestration;
using Finance.Contracts.Events;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Finance.AgentService.API.Controllers;

[ApiController]
[Route("api/agents")]
[Authorize]
public class AgentsController : ControllerBase
{
    private readonly IPortfolioReviewOrchestrator _orchestrator;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<AgentsController> _logger;

    public AgentsController(
        IPortfolioReviewOrchestrator orchestrator,
        IPublishEndpoint publishEndpoint,
        ILogger<AgentsController> logger)
    {
        _orchestrator = orchestrator;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    /// <summary>
    /// Manually triggers the full multi-agent portfolio review pipeline without Service Bus.
    /// Useful for testing and on-demand generation from the dashboard.
    /// </summary>
    [HttpPost("run-portfolio-review")]
    public async Task<IActionResult> RunPortfolioReview(
        [FromBody] RunPortfolioReviewRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized();

        if (request.Tickers is not { Length: > 0 })
            return BadRequest("At least one ticker is required.");

        _logger.LogInformation(
            "Manual portfolio review triggered by user {UserId} for {Tickers}",
            userId, string.Join(", ", request.Tickers));

        var result = await _orchestrator.RunAsync(userId, request.Tickers, cancellationToken);

        return Accepted(new
        {
            reviewId = result.ReviewId,
            generatedAt = result.GeneratedAt,
            message = "Portfolio review completed. Notification will appear on the dashboard."
        });
    }

    /// <summary>
    /// Publishes a PortfolioReviewRequested event to Service Bus to trigger the pipeline asynchronously.
    /// </summary>
    [HttpPost("schedule-portfolio-review")]
    public async Task<IActionResult> SchedulePortfolioReview(
        [FromBody] RunPortfolioReviewRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized();

        if (request.Tickers is not { Length: > 0 })
            return BadRequest("At least one ticker is required.");

        await _publishEndpoint.Publish(
            new PortfolioReviewRequested(userId, request.Tickers),
            cancellationToken);

        return Accepted(new { message = "Portfolio review scheduled. Check your notifications shortly." });
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirst("uid")?.Value;
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }
}

public record RunPortfolioReviewRequest(string[] Tickers);
