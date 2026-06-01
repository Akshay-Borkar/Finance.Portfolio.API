namespace Finance.SentimentService.Infrastructure.Constants;

public static class SentimentConstants
{
    public const string ServiceName = "sentiment";

    public static class Config
    {
        public const string MarketAuxApiToken = "MarketAux:ApiToken";
        public const string MarketAuxBaseUrl = "https://api.marketaux.com/v1/news/all";
    }
}
