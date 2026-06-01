using System.Text.Json;
using Finance.SentimentService.Infrastructure.Constants;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Finance.SentimentService.Infrastructure.Services;

public class MarketAuxService : IMarketAuxService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MarketAuxService> _logger;
    private readonly string _apiToken;

    private const string BaseUrl = SentimentConstants.Config.MarketAuxBaseUrl;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public MarketAuxService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<MarketAuxService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiToken = configuration[SentimentConstants.Config.MarketAuxApiToken]
            ?? throw new InvalidOperationException(
                "MarketAux:ApiToken is not configured. Add it to user secrets or environment variables.");
    }

    public async Task<List<string>> FetchLatestStockNews(string ticker)
    {
        // MarketAux accepts NSE/BSE tickers in the format used by the caller (e.g. TCS.NS, RELIANCE.NS)
        var url = $"{BaseUrl}?symbols={Uri.EscapeDataString(ticker)}&filter_entities=true&language=en&api_token={_apiToken}";

        try
        {
            var response = await _httpClient.GetAsync(url).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var result = JsonSerializer.Deserialize<MarketAuxResponse>(json, JsonOptions);

            if (result?.Data is null or { Count: 0 })
            {
                _logger.LogWarning("MarketAux returned no articles for ticker {Ticker}", ticker);
                return [];
            }

            // Return headline; fall back to description if title is empty
            return result.Data
                .Select(a => string.IsNullOrWhiteSpace(a.Title) ? a.Description : a.Title)
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .ToList();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching news from MarketAux for ticker {Ticker}", ticker);
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize MarketAux response for ticker {Ticker}", ticker);
            throw;
        }
    }
}
