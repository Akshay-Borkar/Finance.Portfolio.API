using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Finance.AgentService.Infrastructure.Clients;

public class MarketAuxClient : IMarketAuxClient
{
    private readonly HttpClient _http;
    private readonly string _apiToken;
    private readonly ILogger<MarketAuxClient> _logger;

    private const string BaseUrl = "https://api.marketaux.com/v1/news/all";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public MarketAuxClient(HttpClient http, IConfiguration configuration, ILogger<MarketAuxClient> logger)
    {
        _http = http;
        _logger = logger;
        _apiToken = configuration["MarketAux:ApiToken"]
            ?? throw new InvalidOperationException("MarketAux:ApiToken is not configured.");
    }

    public async Task<List<string>> FetchLatestStockNewsAsync(string ticker, CancellationToken ct = default)
    {
        var url = $"{BaseUrl}?symbols={Uri.EscapeDataString(ticker)}&filter_entities=true&language=en&api_token={_apiToken}";

        try
        {
            var response = await _http.GetAsync(url, ct).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            var result = JsonSerializer.Deserialize<MarketAuxResponse>(json, JsonOptions);

            if (result?.Data is null or { Count: 0 })
            {
                _logger.LogWarning("MarketAux returned no articles for ticker {Ticker}", ticker);
                return [];
            }

            return result.Data
                .Select(a => string.IsNullOrWhiteSpace(a.Title) ? a.Description : a.Title)
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .ToList()!;
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException)
        {
            _logger.LogError(ex, "Error fetching news from MarketAux for ticker {Ticker}", ticker);
            return [];
        }
    }

    private sealed record MarketAuxResponse(List<MarketAuxArticle>? Data);
    private sealed record MarketAuxArticle(string? Title, string? Description);
}
