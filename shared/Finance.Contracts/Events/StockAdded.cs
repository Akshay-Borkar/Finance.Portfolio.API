namespace Finance.Contracts.Events;

public record StockAdded(Guid UserId, string Ticker, DateTime OccurredAt);
