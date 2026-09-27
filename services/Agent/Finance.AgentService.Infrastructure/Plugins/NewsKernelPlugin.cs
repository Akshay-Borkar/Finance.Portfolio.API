using System.Text.Json;
using System.ComponentModel;
using Finance.Integrations.MarketAux;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace Finance.AgentService.Infrastructure.Plugins;

public sealed class NewsKernelPlugin(IMarketAuxNewsClient marketAux, ILogger<NewsKernelPlugin> logger)
{
    [KernelFunction("fetch_news")]
    [Description("Fetches the latest financial news headlines for a given stock ticker symbol")]
    public async Task<string> FetchNewsAsync(
        [Description("Stock ticker symbol, e.g. TCS.NS or RELIANCE.NS")] string ticker,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<string> headlines;

        // Fail soft: a news outage degrades the review rather than aborting the agent run. The
        // client itself throws after exhausting its retries, so the decision to continue anyway
        // is made here, where the consequence is understood, instead of inside the shared client.
        try
        {
            headlines = await marketAux.GetHeadlinesAsync(ticker, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException)
        {
            logger.LogError(ex, "Could not fetch MarketAux news for {Ticker}; continuing without it", ticker);
            return $"News for {ticker} is unavailable right now.";
        }

        if (headlines.Count == 0)
            return $"No recent news found for {ticker}.";

        var lines = string.Join("\n- ", headlines);
        return $"Headlines for {ticker}:\n- {lines}";
    }
}
