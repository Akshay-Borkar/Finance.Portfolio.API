namespace Finance.Contracts.Events;

public record StockRemoved(Guid UserId, string Ticker, DateTime OccurredAt);
