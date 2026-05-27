namespace Finance.AgentService.Infrastructure.Orchestration;

public interface IPortfolioReviewOrchestrator
{
    Task<PortfolioReviewResult> RunAsync(Guid userId, string[] tickers, CancellationToken ct = default);
}
