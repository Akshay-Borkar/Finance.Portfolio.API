using Finance.AgentService.Infrastructure.Orchestration;
using Finance.Contracts.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Finance.AgentService.Infrastructure.Consumers;

public class PortfolioReviewRequestedConsumer : IConsumer<PortfolioReviewRequested>
{
    private readonly IPortfolioReviewOrchestrator _orchestrator;
    private readonly ILogger<PortfolioReviewRequestedConsumer> _logger;

    public PortfolioReviewRequestedConsumer(
        IPortfolioReviewOrchestrator orchestrator,
        ILogger<PortfolioReviewRequestedConsumer> logger)
    {
        _orchestrator = orchestrator;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PortfolioReviewRequested> context)
    {
        var msg = context.Message;
        _logger.LogInformation(
            "Received PortfolioReviewRequested for user {UserId} with {Count} tickers",
            msg.UserId, msg.Tickers.Length);

        await _orchestrator.RunAsync(msg.UserId, msg.Tickers, context.CancellationToken);
    }
}
