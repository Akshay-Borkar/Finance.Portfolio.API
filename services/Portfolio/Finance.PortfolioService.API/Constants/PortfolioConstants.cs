namespace Finance.PortfolioService.API.Constants;

public static class PortfolioConstants
{
    public static class Sse
    {
        public const string ContentType = "text/event-stream";
        public const string CacheControlValue = "no-cache";
        public const string ConnectionValue = "keep-alive";
        public const string XAccelBufferingHeader = "X-Accel-Buffering";
        public const string XAccelBufferingValue = "no";
        public const string DoneFrame = "data: [DONE]\n\n";
        public const string ChatNotConfigured = "data: AI chat is not configured.\n\n";
        public const string RebalancingNotConfigured = "data: Rebalancing agent is not configured.\n\n";
    }

    public static class Chart
    {
        public const string DefaultInterval = "5m";
        public const string DefaultRange = "1d";
    }

    public static class Session
    {
        public const string RebalancingKeyFormat = "{0}:rebalancing:{1}";
    }

    public static class Documents
    {
        public const long MaxFileSizeBytes = 20 * 1024 * 1024;
        public const string AllowedExtension = ".pdf";
    }

    public static class Config
    {
        public const string AzureOpenAISection = "AzureOpenAI";
        public const string AzureSearchSection = "AzureSearch";
        public const string MarketDataGrpcAddress = "MarketData:GrpcAddress";
        public const string DefaultGrpcAddress = "http://marketdata-svc:8081";
    }

}
