namespace Finance.MarketDataService.Infrastructure.Constants;

public static class MarketDataConstants
{
    public static class Redis
    {
        public const string ActiveTickersKey = "mkt:subscriptions:active-tickers";
        public const string PriceCacheKeyPrefix = "mkt:price:";
        public const int PriceCacheTtlMinutes = 5;
        public const int ConnectTimeoutMs = 5000;
        public const int SyncTimeoutMs = 5000;
    }

    public const string ServiceName = "marketdata";

    public static class HangfireJobs
    {
        public const string Minutely = "UpdateStockPricesMinutely";
        public const string Hourly = "UpdateStockPricesHourly";
        public const string Daily = "UpdateStockPricesDaily";
        public const string Weekly = "UpdateStockPricesWeekly";
        public const string DashboardRoute = "/hangfire";
    }

    public static class OhlcvDefaults
    {
        public const string Interval = "1d";
        public const string Range = "1mo";
    }

    public static class Config
    {
        public const string RedisEndpoint = "RedisEndpoint";
        public const string RedisPassword = "RedisPassword";
        public const string ServiceBusConnectionString = "ServiceBusConnectionString";
    }
}
