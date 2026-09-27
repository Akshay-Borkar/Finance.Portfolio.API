namespace Finance.Integrations.MarketAux;

public interface IMarketAuxNewsClient
{
    /// <summary>
    /// Latest headlines for a ticker, falling back to an article's description when it has no
    /// title. Returns empty when MarketAux has no articles for the ticker.
    /// </summary>
    /// <exception cref="HttpRequestException">
    /// The request kept failing after <see cref="MarketAuxOptions.MaxAttempts"/> attempts. Callers
    /// that must not fail on a news outage catch this and carry on -- see NewsKernelPlugin.
    /// </exception>
    Task<IReadOnlyList<string>> GetHeadlinesAsync(string ticker, CancellationToken cancellationToken = default);
}
