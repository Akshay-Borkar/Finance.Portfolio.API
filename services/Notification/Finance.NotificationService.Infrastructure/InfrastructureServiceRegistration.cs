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

            // RabbitMQ configuration
            // x.UsingRabbitMq((ctx, cfg) =>
            // {
            //     cfg.Host(
            //         configuration["RabbitMq:Host"] ?? "rabbitmq",
            //         h =>
            //         {
            //             h.Username(configuration["RabbitMq:Username"] ?? "guest");
            //             h.Password(configuration["RabbitMq:Password"] ?? "guest");
            //         });
            //     cfg.ConfigureEndpoints(ctx);
            // });

            // Azure Service Bus configuration
            x.UsingAzureServiceBus((ctx, cfg) =>
            {
                var connectionString = configuration[NotificationConstants.Config.ServiceBusConnectionString];
                if (string.IsNullOrWhiteSpace(connectionString))
                    throw new InvalidOperationException(
                        "ServiceBusConnectionString is not configured. Add it to appsettings or user secrets.");

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
        });

        return services;
    }
}
