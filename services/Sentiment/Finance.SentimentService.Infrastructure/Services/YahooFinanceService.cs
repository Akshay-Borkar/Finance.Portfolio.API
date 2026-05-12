using System.Xml.Linq;

namespace Finance.SentimentService.Infrastructure.Services;

public class YahooFinanceService : IYahooFinanceService
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "https://feeds.finance.yahoo.com/rss/2.0/headline?s={0}&region=US&lang=en-US";

    public YahooFinanceService(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<List<string>> FetchLatestStockNews(string ticker)
    {
        var url = string.Format(BaseUrl, ticker);
        var xml = await _httpClient.GetStringAsync(url);

        return XDocument.Parse(xml)
            .Descendants("item")
            .Select(i => i.Element("title")?.Value)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Cast<string>()
            .ToList();
    }
}
