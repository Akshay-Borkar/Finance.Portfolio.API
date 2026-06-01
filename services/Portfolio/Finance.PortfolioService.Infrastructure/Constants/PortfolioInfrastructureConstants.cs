namespace Finance.PortfolioService.Infrastructure.Constants;

public static class PortfolioInfrastructureConstants
{
    public static class Config
    {
        public const string MarketDataGrpcAddress = "MarketData:GrpcAddress";
        public const string DefaultGrpcAddress = "http://marketdata-svc:8081";
        public const string AzureOpenAISection = "AzureOpenAI";
        public const string AzureSearchSection = "AzureSearch";
        public const string ServiceBusConnectionString = "ServiceBusConnectionString";
    }

    public static class AI
    {
        public const int SearchTopK = 5;
        public const int IngestionBatchSize = 16;
        public const int ChunkTargetWords = 500;
        public const int ChunkOverlapWords = 50;
    }
}
