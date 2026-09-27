using Finance.Contracts.Events;
using Finance.NotificationService.Infrastructure.Constants;
using Finance.NotificationService.Infrastructure.Consumers;
using Finance.NotificationService.Infrastructure.Email;
using Finance.SharedKernel.Messaging;
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

        services.AddSharedMessaging(
            configuration,
            x =>
            {
                x.AddConsumer<StockPriceUpdatedConsumer>();
                x.AddConsumer<AlertTriggeredConsumer>();
                x.AddConsumer<PortfolioReviewCompletedConsumer>();
            },
            // All three consumers get an explicit SubscriptionEndpoint so MassTransit creates
            // predictable topic/subscription pairs on Azure Service Bus. Topic is derived from the
            // message type (kebab-case simple name):
            //   StockPriceUpdated        → topic: stock-price-updated
            //   AlertTriggered           → topic: alert-triggered
            //   PortfolioReviewCompleted → topic: portfolio-review-completed
            (cfg, ctx) =>
            {
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

        return services;
    }
}
