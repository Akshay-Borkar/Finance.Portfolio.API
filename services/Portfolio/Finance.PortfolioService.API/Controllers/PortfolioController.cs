using Finance.PortfolioService.API.Constants;
using Finance.PortfolioService.API.Models;
using Finance.SharedKernel.Auth;
using Finance.PortfolioService.Application.Common;
using Finance.PortfolioService.Application.Contracts.AI;
using Finance.PortfolioService.Application.Contracts.MarketData;
using Finance.PortfolioService.Application.Features.Portfolio.Commands.AddInvestment;
using Finance.PortfolioService.Application.Features.Portfolio.Commands.AddStock;
using Finance.PortfolioService.Application.Features.Portfolio.Commands.DeleteStock;
using Finance.PortfolioService.Application.Features.Portfolio.Queries.ChatWithPortfolio;
using Finance.PortfolioService.Application.Features.Portfolio.Queries.GetInvestmentsByStockId;
using Finance.PortfolioService.Application.Features.Portfolio.Queries.GetPortfolioSummary;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Finance.PortfolioService.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class PortfolioController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IMarketDataGrpcClient _marketData;
    private readonly IPortfolioChatService? _chatService;
    private readonly IRebalancingAgentService? _rebalancingAgent;

    public PortfolioController(
        IMediator mediator,
        IMarketDataGrpcClient marketData,
        IPortfolioChatService? chatService = null,
        IRebalancingAgentService? rebalancingAgent = null)
    {
        _mediator = mediator;
        _marketData = marketData;
        _chatService = chatService;
        _rebalancingAgent = rebalancingAgent;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<PortfolioSummaryDto>> GetSummary()
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var result = await _mediator.Send(new GetPortfolioSummaryQuery(userId));
        return Ok(result);
    }

    [HttpPost("stock")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> AddStock([FromBody] AddStockCommand command)
    {
        command.UserId = GetUserId();
        if (command.UserId == Guid.Empty) return Unauthorized();

        var id = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetSummary), new { }, new { id });
    }

    [HttpPost("investment")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> AddInvestment([FromBody] AddInvestmentCommand command)
    {
        command.UserId = GetUserId();
        if (command.UserId == Guid.Empty) return Unauthorized();

        var id = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetSummary), new { }, new { id });
    }

    [HttpDelete("stock/{stockId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteStock(Guid stockId)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        await _mediator.Send(new DeleteStockCommand(stockId, userId));
        return NoContent();
    }

    [HttpGet("chart/{ticker}")]
    public async Task<ActionResult<List<OhlcvBarDto>>> GetChart(
        string ticker,
        [FromQuery] string interval = PortfolioConstants.Chart.DefaultInterval,
        [FromQuery] string range = PortfolioConstants.Chart.DefaultRange)
    {
        var bars = await _marketData.GetOhlcvAsync(ticker, interval, range);
        return Ok(bars);
    }

    [HttpGet("investments/{stockId:guid}")]
    public async Task<ActionResult<PagedResult<InvestmentHistoryDto>>> GetInvestments(
        Guid stockId,
        [FromQuery] int page = AuthConstants.Pagination.DefaultPage,
        [FromQuery] int pageSize = AuthConstants.Pagination.DefaultPageSize)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var result = await _mediator.Send(new GetInvestmentsByStockIdQuery(stockId, userId, page, pageSize));
        return Ok(result);
    }

    [HttpPost("chat")]
    [Authorize]
    public async Task ChatWithPortfolio([FromBody] ChatRequest request, CancellationToken cancellationToken)
    {
        if (_chatService is null)
        {
            Response.StatusCode = 503;
            await Response.WriteAsync(PortfolioConstants.Sse.ChatNotConfigured, cancellationToken);
            return;
        }

        Response.ContentType = PortfolioConstants.Sse.ContentType;
        Response.Headers.CacheControl = PortfolioConstants.Sse.CacheControlValue;
        Response.Headers.Connection = PortfolioConstants.Sse.ConnectionValue;

        var userId = GetUserId();
        if (userId == Guid.Empty)
        {
            Response.StatusCode = 401;
            return;
        }

        var stream = await _mediator.Send(new ChatWithPortfolioQuery(request.Message, userId), cancellationToken);

        await foreach (var chunk in stream.WithCancellation(cancellationToken))
        {
            await Response.WriteAsync($"data: {chunk}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }

        await Response.WriteAsync(PortfolioConstants.Sse.DoneFrame, cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
    }

    [HttpPost("ai/rebalancing/chat")]
    [Authorize]
    public async Task RebalancingChat([FromBody] RebalancingChatRequest request, CancellationToken cancellationToken)
    {
        if (_rebalancingAgent is null)
        {
            Response.StatusCode = 503;
            await Response.WriteAsync(PortfolioConstants.Sse.RebalancingNotConfigured, cancellationToken);
            return;
        }

        var userId = GetUserId();
        if (userId == Guid.Empty)
        {
            Response.StatusCode = 401;
            return;
        }

        var sessionId = string.IsNullOrWhiteSpace(request.SessionId)
            ? string.Format(PortfolioConstants.Session.RebalancingKeyFormat, userId, Guid.NewGuid())
            : request.SessionId;

        Response.ContentType = PortfolioConstants.Sse.ContentType;
        Response.Headers.CacheControl = PortfolioConstants.Sse.CacheControlValue;
        Response.Headers[PortfolioConstants.Sse.XAccelBufferingHeader] = PortfolioConstants.Sse.XAccelBufferingValue;

        await foreach (var chunk in _rebalancingAgent.ChatAsync(request.Message, userId, sessionId, cancellationToken))
        {
            await Response.WriteAsync($"data: {chunk}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }

        await Response.WriteAsync(PortfolioConstants.Sse.DoneFrame, cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
    }

    [HttpDelete("ai/rebalancing/session/{sessionId}")]
    [Authorize]
    public async Task<ActionResult> ClearRebalancingSession(string sessionId)
    {
        if (_rebalancingAgent is null)
            return StatusCode(503);

        await _rebalancingAgent.ClearSessionAsync(sessionId);
        return Ok();
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirst(AuthConstants.Claims.UserId)?.Value;
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }
}
