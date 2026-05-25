using Finance.SentimentService.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Finance.SentimentService.Infrastructure;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddSentimentInfrastructure(this IServiceCollection services)
    {
        services.AddHttpClient<IMarketAuxService, MarketAuxService>();
        services.AddSingleton<ISentimentAnalysisService, SentimentAnalysisService>();

        return services;
    }
}
