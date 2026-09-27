namespace Finance.AgentService.Infrastructure.Constants;

public static class AgentConstants
{
    public static class ServiceBus
    {
        public const string SubscriptionName = "finance-agentservice";
    }

    public static class Config
    {
        public const string AzureOpenAISection = "AzureOpenAI";
        public const string SentimentServiceBaseUrl = "SentimentService:BaseUrl";
    }
}
