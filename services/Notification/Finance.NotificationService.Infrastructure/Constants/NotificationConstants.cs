namespace Finance.NotificationService.Infrastructure.Constants;

public static class NotificationConstants
{
    public static class Hubs
    {
        public const string StockPriceRoute = "/hubs/stockprice";
        public const string PortfolioReviewRoute = "/hubs/portfolio-review";
    }

    public static class SignalRMethods
    {
        public const string ReceiveStockPrice = "ReceiveStockPrice";
        public const string ReceivePortfolioReview = "ReceivePortfolioReview";
        public const string SubscribeToStock = "SubscribeToStock";
        public const string UnsubscribeFromStock = "UnsubscribeFromStock";
    }

    public static class ServiceBus
    {
        public const string SubscriptionName = "finance-notificationservice";
    }

    public static class Config
    {
        public const string RedisEndpoint = "RedisEndpoint";
        public const string RedisPassword = "RedisPassword";
        public const string ServiceBusConnectionString = "ServiceBusConnectionString";
        public const string EmailSettings = "EmailSettings";
        public const int RedisConnectTimeoutMs = 5000;
        public const int RedisSyncTimeoutMs = 5000;
    }
}
