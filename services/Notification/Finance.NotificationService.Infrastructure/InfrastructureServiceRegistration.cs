using Finance.Contracts.Events;
using Finance.NotificationService.Infrastructure.Constants;
using Finance.NotificationService.Infrastructure.Consumers;
using Finance.NotificationService.Infrastructure.Email;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Finance.NotificationService.Infrastructure;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<EmailSettings>(configuration.GetSection(NotificationConstants.Config.EmailSettings));
        services.AddSingleton<EmailSender>();

        services.AddMassTransit(x =>
        {
            x.AddConsumer<StockPriceUpdatedConsumer>();
            x.AddConsumer<AlertTriggeredConsumer>();
            x.AddConsumer<PortfolioReviewCompletedConsumer>();

            // Transport is selected at startup based on configuration: RabbitMq:Host wins when
            // present (local/dev via docker-compose), otherwise falls back to Azure Service Bus
            // (staging/prod). Both branches stay wired so switching is a config change, not a code change.
            var rabbitMqHost = configuration["RabbitMq:Host"];
            if (!string.IsNullOrWhiteSpace(rabbitMqHost))
            {
                // RabbitMQ configuration — ConfigureEndpoints' convention-based queue naming is
                // fine here; the explicit SubscriptionEndpoint naming below is only needed to
                // control Azure Service Bus topic/subscription names.
                x.UsingRabbitMq((ctx, cfg) =>
                {
                    cfg.Host(rabbitMqHost, h =>
                    {
                        h.Username(configuration["RabbitMq:Username"] ?? "guest");
                        h.Password(configuration["RabbitMq:Password"] ?? "guest");
                    });
                    cfg.ConfigureEndpoints(ctx);
                });
            }
            else
            {
                // Azure Service Bus configuration
                x.UsingAzureServiceBus((ctx, cfg) =>
                {
                    var connectionString = configuration[NotificationConstants.Config.ServiceBusConnectionString];
                    if (string.IsNullOrWhiteSpace(connectionString))
                        throw new InvalidOperationException(
                            "Neither RabbitMq:Host nor ServiceBusConnectionString is configured. Set one to enable messaging.");

                    cfg.Host(connectionString);

                    // All three consumers are wired with SubscriptionEndpoint so MassTransit creates
                    // predictable topic/subscription pairs on Azure Service Bus without relying on
                    // ConfigureEndpoints convention-based naming (which would suffix "-consumer").
                    //
                    // Topic derived from message type (kebab-case simple name):
                    //   StockPriceUpdated        → topic: stock-price-updated
                    //   AlertTriggered           → topic: alert-triggered
                    //   PortfolioReviewCompleted → topic: portfolio-review-completed
                    cfg.SubscriptionEndpoint<StockPriceUpdated>(
                        NotificationConstants.ServiceBus.SubscriptionName,
                        e => e.ConfigureConsumer<StockPriceUpdatedConsumer>(ctx));

                    cfg.SubscriptionEndpoint<AlertTriggered>(
                        NotificationConstants.ServiceBus.SubscriptionName,
                        e => e.ConfigureConsumer<AlertTriggeredConsumer>(ctx));

                    cfg.SubscriptionEndpoint<PortfolioReviewCompleted>(
                        NotificationConstants.ServiceBus.SubscriptionName,
                        e => e.ConfigureConsumer<PortfolioReviewCompletedConsumer>(ctx));
                });
            }
        });

        return services;
    }
}
