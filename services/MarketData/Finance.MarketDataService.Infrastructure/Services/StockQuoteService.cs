using Finance.MarketDataService.Application.Contracts;
using Finance.MarketDataService.Application.Models;
using Newtonsoft.Json;

namespace Finance.MarketDataService.Infrastructure.Services;

public class StockQuoteService : IStockQuoteService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private const string ChartUrl = "https://query1.finance.yahoo.com/v8/finance/chart/{0}";

    public StockQuoteService(IHttpClientFactory httpClientFactory) =>
        _httpClientFactory = httpClientFactory;

    public async Task<StockApiResponse?> FetchStockQuoteAsync(string ticker, CancellationToken cancellationToken = default)
    {
        using var client = BuildClient();
        var response = await client.GetAsync(string.Format(ChartUrl, ticker), cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return JsonConvert.DeserializeObject<StockApiResponse>(content);
    }

    public async Task<List<OhlcvBar>> FetchOhlcvAsync(string ticker, string interval, string range, CancellationToken cancellationToken = default)
    {
        using var client = BuildClient();
        var url = $"{string.Format(ChartUrl, ticker)}?interval={interval}&range={range}";
        var response = await client.GetAsync(url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var parsed = JsonConvert.DeserializeObject<StockApiResponse>(content);

        var result = parsed?.Chart?.Result?.FirstOrDefault();
        if (result is null || result.Timestamp.Count == 0) return [];

        var quote = result.Indicators.Quote.FirstOrDefault();
        if (quote is null) return [];

        var bars = new List<OhlcvBar>();
        for (int i = 0; i < result.Timestamp.Count; i++)
        {
            if (i >= quote.Open.Count) break;
            var o = quote.Open[i]; var h = quote.High[i];
            var l = quote.Low[i];  var c = quote.Close[i];
            if (o is null || h is null || l is null || c is null) continue;

            bars.Add(new OhlcvBar
            {
                Time   = result.Timestamp[i],
                Open   = o.Value, High = h.Value,
                Low    = l.Value, Close = c.Value,
                Volume = quote.Volume.Count > i ? quote.Volume[i] ?? 0 : 0
            });
        }
        return bars;
    }

    private HttpClient BuildClient()
    {
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add(
            "User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:89.0) Gecko/20100101 Firefox/89.0");
        return client;
    }
}
