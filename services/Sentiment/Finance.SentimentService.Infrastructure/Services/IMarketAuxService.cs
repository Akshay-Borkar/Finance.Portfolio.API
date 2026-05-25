namespace Finance.SentimentService.Infrastructure.Services;

public interface IMarketAuxService
{
    Task<List<string>> FetchLatestStockNews(string ticker);
}
