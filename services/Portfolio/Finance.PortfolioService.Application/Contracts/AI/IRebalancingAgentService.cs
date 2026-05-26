namespace Finance.PortfolioService.Application.Contracts.AI;

public interface IRebalancingAgentService
{
    IAsyncEnumerable<string> ChatAsync(string userMessage, Guid userId, string sessionId, CancellationToken cancellationToken);
    Task ClearSessionAsync(string sessionId);
}
