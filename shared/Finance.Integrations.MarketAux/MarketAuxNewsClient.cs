using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Finance.Integrations.MarketAux;

public class MarketAuxNewsClient : IMarketAuxNewsClient
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _http;
    private readonly MarketAuxOptions _options;
    private readonly ILogger<MarketAuxNewsClient> _logger;

    public MarketAuxNewsClient(
        HttpClient http,
        IOptions<MarketAuxOptions> options,
        ILogger<MarketAuxNewsClient> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;

        if (string.IsNullOrWhiteSpace(_options.ApiToken))
            throw new InvalidOperationException(
                $"{MarketAuxOptions.SectionName}:ApiToken is not configured. Add it to user secrets, " +
                "environment variables or Key Vault.");
    }

    public async Task<IReadOnlyList<string>> GetHeadlinesAsync(string ticker, CancellationToken cancellationToken = default)
    {
        // MarketAux takes the ticker in whatever form the caller uses, including the NSE/BSE
        // suffixes this app passes through (TCS.NS, RELIANCE.NS).
        var url = $"{_options.BaseUrl}?symbols={Uri.EscapeDataString(ticker)}" +
                  $"&filter_entities=true&language=en&api_token={_options.ApiToken}";

        for (var attempt = 1; attempt <= _options.MaxAttempts; attempt++)
        {
            try
            {
                using var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                var result = JsonSerializer.Deserialize<MarketAuxResponse>(json, JsonOptions);

                if (result?.Data is null or { Count: 0 })
                {
                    _logger.LogWarning("MarketAux returned no articles for ticker {Ticker}", ticker);
                    return [];
                }

                return result.Data
                    .Select(a => string.IsNullOrWhiteSpace(a.Title) ? a.Description : a.Title)
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .ToList();
            }
            catch (HttpRequestException ex) when (attempt < _options.MaxAttempts)
            {
                var delay = _options.RetryBackoff * attempt;
                _logger.LogWarning(
                    ex,
                    "Transient error fetching MarketAux news for {Ticker}, retrying in {Delay}ms ({Attempt}/{MaxAttempts})",
                    ticker, delay.TotalMilliseconds, attempt, _options.MaxAttempts);

                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
            catch (JsonException ex)
            {
                // Not transient -- a malformed body will be malformed on every retry.
                _logger.LogError(ex, "Failed to deserialize MarketAux response for ticker {Ticker}", ticker);
                throw;
            }
        }

        throw new HttpRequestException(
            $"Failed to fetch MarketAux news for {ticker} after {_options.MaxAttempts} attempts.");
    }

    private sealed class MarketAuxResponse
    {
        [JsonPropertyName("data")]
        public List<MarketAuxArticle> Data { get; set; } = [];
    }

    private sealed class MarketAuxArticle
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;
    }
}
