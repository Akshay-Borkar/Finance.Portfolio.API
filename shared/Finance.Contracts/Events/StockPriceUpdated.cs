namespace Finance.Contracts.Events;

public record StockPriceUpdated(string Ticker, decimal Price, long UnixTimestamp);
