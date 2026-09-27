namespace Finance.Integrations.MarketAux;

public class MarketAuxOptions
{
    public const string SectionName = "MarketAux";

    public string ApiToken { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = "https://api.marketaux.com/v1/news/all";

    /// <summary>
    /// Total attempts, including the first. Sentiment previously retried 15 times with no delay
    /// between attempts, which hammers a rate-limited endpoint rather than riding out a blip.
    /// </summary>
    public int MaxAttempts { get; set; } = 3;

    /// <summary>Delay before the first retry; grows linearly with each subsequent attempt.</summary>
    public TimeSpan RetryBackoff { get; set; } = TimeSpan.FromMilliseconds(500);
}
