using System.ComponentModel;
using Finance.AgentService.Infrastructure.Clients;
using Microsoft.SemanticKernel;

namespace Finance.AgentService.Infrastructure.Plugins;

public sealed class NewsKernelPlugin(IMarketAuxClient marketAux)
{
    [KernelFunction("fetch_news")]
    [Description("Fetches the latest financial news headlines for a given stock ticker symbol")]
    public async Task<string> FetchNewsAsync(
        [Description("Stock ticker symbol, e.g. TCS.NS or RELIANCE.NS")] string ticker,
        CancellationToken cancellationToken = default)
    {
        var headlines = await marketAux.FetchLatestStockNewsAsync(ticker, cancellationToken);

        if (headlines.Count == 0)
            return $"No recent news found for {ticker}.";

        var lines = string.Join("\n- ", headlines);
        return $"Headlines for {ticker}:\n- {lines}";
    }
}
