namespace Finance.Contracts.Events;

public record AlertTriggered(
    Guid AlertId,
    Guid UserId,
    string UserEmail,
    string Ticker,
    decimal TargetPrice,
    decimal CurrentPrice,
    string Direction);
