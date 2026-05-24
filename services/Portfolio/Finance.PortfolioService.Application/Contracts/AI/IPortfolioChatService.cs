namespace Finance.PortfolioService.Application.Contracts.AI;

public interface IPortfolioChatService
{
    IAsyncEnumerable<string> StreamChatAsync(
        string userMessage,
        Guid userId,
        CancellationToken cancellationToken);
}
