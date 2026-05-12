using Finance.MarketDataService.Application.Models;

namespace Finance.MarketDataService.Application.Contracts;

public interface IStockQuoteService
{
    Task<StockApiResponse?> FetchStockQuoteAsync(string ticker);
    Task<List<OhlcvBar>>    FetchOhlcvAsync(string ticker, string interval, string range);
}
