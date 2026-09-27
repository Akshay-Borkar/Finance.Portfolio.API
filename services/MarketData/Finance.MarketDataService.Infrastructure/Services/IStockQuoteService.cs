using Finance.MarketDataService.Infrastructure.Models;

namespace Finance.MarketDataService.Infrastructure.Services;

public interface IStockQuoteService
{
    Task<StockApiResponse?> FetchStockQuoteAsync(string ticker, CancellationToken cancellationToken = default);
    Task<List<OhlcvBar>>    FetchOhlcvAsync(string ticker, string interval, string range, CancellationToken cancellationToken = default);
}
