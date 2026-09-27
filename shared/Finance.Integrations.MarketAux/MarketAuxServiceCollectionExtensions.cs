using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Finance.Integrations.MarketAux;

public static class MarketAuxServiceCollectionExtensions
{
    /// <summary>
    /// Binds the <c>MarketAux</c> configuration section and registers
    /// <see cref="IMarketAuxNewsClient"/> on its own <see cref="HttpClient"/>.
    /// </summary>
    public static IServiceCollection AddMarketAuxNews(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<MarketAuxOptions>()
            .Bind(configuration.GetSection(MarketAuxOptions.SectionName));

        services.AddHttpClient<IMarketAuxNewsClient, MarketAuxNewsClient>();

        return services;
    }
}
