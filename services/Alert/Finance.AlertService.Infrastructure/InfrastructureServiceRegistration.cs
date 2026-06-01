using Finance.AlertService.Infrastructure.Consumers;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Finance.AlertService.Infrastructure;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMassTransit(x =>
        {
            x.AddConsumer<StockPriceUpdatedConsumer>();

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
                var connectionString = configuration[AlertConstants.Config.ServiceBusConnectionString];
                if (string.IsNullOrWhiteSpace(connectionString))
                    throw new InvalidOperationException(
                        "ServiceBusConnectionString is not configured. Add it to appsettings or user secrets.");

                cfg.Host(connectionString);
                cfg.ConfigureEndpoints(ctx);
            });
        });

        return services;
    }
}
