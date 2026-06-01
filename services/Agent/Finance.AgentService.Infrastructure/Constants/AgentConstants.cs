namespace Finance.AgentService.Infrastructure.Constants;

public static class AgentConstants
{
    public static class ServiceBus
    {
        public const string SubscriptionName = "finance-agentservice";
        public const string ServiceBusConnectionString = "ServiceBusConnectionString";
    }

    public static class Config
    {
        public const string AzureOpenAISection = "AzureOpenAI";
        public const string MarketAuxApiToken = "MarketAux:ApiToken";
        public const string MarketAuxBaseUrl = "https://api.marketaux.com/v1/news/all";
        public const string SentimentServiceBaseUrl = "SentimentService:BaseUrl";
    }
}
