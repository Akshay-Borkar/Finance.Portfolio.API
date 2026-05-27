namespace Finance.Contracts.Events;

public record PortfolioReviewRequested(Guid UserId, string[] Tickers);
