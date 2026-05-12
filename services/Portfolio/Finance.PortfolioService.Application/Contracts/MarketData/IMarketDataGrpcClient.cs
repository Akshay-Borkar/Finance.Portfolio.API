namespace Finance.PortfolioService.Application.Contracts.MarketData;

public interface IMarketDataGrpcClient
{
    Task<decimal> GetCurrentPriceAsync(string ticker, CancellationToken cancellationToken = default);
    Task<List<OhlcvBarDto>> GetOhlcvAsync(string ticker, string interval, string range, CancellationToken cancellationToken = default);
}

public record OhlcvBarDto(long Time, double Open, double High, double Low, double Close, long Volume);
