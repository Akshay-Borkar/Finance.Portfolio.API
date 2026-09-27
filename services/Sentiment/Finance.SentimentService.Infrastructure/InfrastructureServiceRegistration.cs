using Finance.Integrations.MarketAux;
using Finance.SentimentService.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Finance.SentimentService.Infrastructure;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddSentimentInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddMarketAuxNews(configuration);
        services.AddSingleton<ISentimentAnalysisService, SentimentAnalysisService>();

        return services;
    }
}
