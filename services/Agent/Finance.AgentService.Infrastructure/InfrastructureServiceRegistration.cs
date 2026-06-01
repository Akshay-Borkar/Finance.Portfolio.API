using Finance.AgentService.Infrastructure.Clients;
using Finance.AgentService.Infrastructure.Constants;
using Finance.AgentService.Infrastructure.Consumers;
using Finance.AgentService.Infrastructure.Orchestration;
using Finance.AgentService.Infrastructure.Settings;
using Finance.Contracts.Events;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Finance.AgentService.Infrastructure;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHttpClient<IMarketAuxClient, MarketAuxClient>();
        services.AddHttpClient<ISentimentApiClient, SentimentApiClient>();

        services.Configure<AzureAISettings>(configuration.GetSection(AgentConstants.Config.AzureOpenAISection));

        services.AddSingleton<IAgentCache>(sp =>
        {
            var aiSettings = configuration.GetSection(AgentConstants.Config.AzureOpenAISection).Get<AzureAISettings>()
                ?? throw new InvalidOperationException("AzureOpenAI configuration section is missing.");

            if (!aiSettings.IsConfigured)
                throw new InvalidOperationException(
                    "AzureOpenAI:Endpoint and AzureOpenAI:ApiKey are not configured. Add them to user secrets or Key Vault.");

            return new AgentCache(
                aiSettings,
                sp.GetRequiredService<IMarketAuxClient>(),
                sp.GetRequiredService<ISentimentApiClient>(),
                sp.GetRequiredService<ILogger<AgentCache>>());
        });

        services.AddScoped<IPortfolioReviewOrchestrator, PortfolioReviewOrchestrator>();

        services.AddMassTransit(x =>
        {
            x.AddConsumer<PortfolioReviewRequestedConsumer>();

            // RabbitMQ configuration (for local dev without Service Bus)
            // x.UsingRabbitMq((ctx, cfg) =>
            // {
            //     cfg.Host(configuration["RabbitMq:Host"] ?? "rabbitmq", h =>
            //     {
            //         h.Username(configuration["RabbitMq:Username"] ?? "guest");
            //         h.Password(configuration["RabbitMq:Password"] ?? "guest");
            //     });
            //     cfg.ConfigureEndpoints(ctx);
            // });

            x.UsingAzureServiceBus((ctx, cfg) =>
            {
                var connectionString = configuration[AgentConstants.ServiceBus.ServiceBusConnectionString];
                if (string.IsNullOrWhiteSpace(connectionString))
                    throw new InvalidOperationException(
                        "ServiceBusConnectionString is not configured. Add it to appsettings or user secrets.");

                cfg.Host(connectionString);

                // Explicit subscription name on topic "portfolio-review-requested".
                // MassTransit derives the topic from the message type (Finance.Contracts.Events.PortfolioReviewRequested
                // → "portfolio-review-requested"). The subscription name must be set explicitly; otherwise MassTransit
                // defaults to the consumer class name in kebab-case, which would be
                // "portfolio-review-requested-consumer" — not what Azure Service Bus expects.
                cfg.SubscriptionEndpoint<PortfolioReviewRequested>(
                    AgentConstants.ServiceBus.SubscriptionName,
                    e => e.ConfigureConsumer<PortfolioReviewRequestedConsumer>(ctx));
            });
        });

        return services;
    }
}
