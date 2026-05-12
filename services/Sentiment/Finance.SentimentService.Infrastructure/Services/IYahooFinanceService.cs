namespace Finance.SentimentService.Infrastructure.Services;

public interface IYahooFinanceService
{
    Task<List<string>> FetchLatestStockNews(string ticker);
}
