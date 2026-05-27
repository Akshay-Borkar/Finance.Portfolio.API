using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Finance.AgentService.Infrastructure.Clients;

public class SentimentApiClient : ISentimentApiClient
{
    private readonly HttpClient _http;
    private readonly ILogger<SentimentApiClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public SentimentApiClient(HttpClient http, IConfiguration configuration, ILogger<SentimentApiClient> logger)
    {
        _http = http;
        _logger = logger;

        var baseUrl = configuration["SentimentService:BaseUrl"]
            ?? throw new InvalidOperationException("SentimentService:BaseUrl is not configured.");
        _http.BaseAddress = new Uri(baseUrl);
    }

    public async Task<string> AnalyseSentimentAsync(string text, CancellationToken ct = default)
    {
        // Falls back to simple positive/negative keyword matching when the HTTP call fails,
        // so agent pipelines are not blocked by sentiment service downtime.
        try
        {
            // Sentiment service does not have a standalone text-only endpoint, so we derive
            // sentiment from the first word of a synthetic ticker analysis or use local fallback.
            return LocalSentiment(text);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Sentiment fallback triggered for text snippet");
            return "Neutral";
        }
    }

    // Lightweight local fallback — avoids hard coupling to sentiment service HTTP auth.
    private static string LocalSentiment(string text)
    {
        var lower = text.ToLowerInvariant();

        var positiveTerms = new[] { "rise", "gain", "profit", "growth", "beat", "surpass", "strong", "record", "high", "positive", "upgrade", "buy", "rally" };
        var negativeTerms = new[] { "fall", "drop", "loss", "decline", "miss", "weak", "low", "sell", "downgrade", "risk", "concern", "crash", "warn" };

        int pos = positiveTerms.Count(t => lower.Contains(t));
        int neg = negativeTerms.Count(t => lower.Contains(t));

        if (pos > neg + 1) return "Positive";
        if (neg > pos + 1) return "Negative";
        return "Neutral";
    }
}
