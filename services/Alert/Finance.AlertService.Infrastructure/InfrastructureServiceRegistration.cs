using Finance.AlertService.Infrastructure.Constants;
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

            // Transport is selected at startup based on configuration: RabbitMq:Host wins when
            // present (local/dev via docker-compose), otherwise falls back to Azure Service Bus
            // (staging/prod). Both branches stay wired so switching is a config change, not a code change.
            var rabbitMqHost = configuration["RabbitMq:Host"];
            if (!string.IsNullOrWhiteSpace(rabbitMqHost))
            {
                // RabbitMQ configuration
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
                    var connectionString = configuration[AlertConstants.Config.ServiceBusConnectionString];
                    if (string.IsNullOrWhiteSpace(connectionString))
                        throw new InvalidOperationException(
                            "Neither RabbitMq:Host nor ServiceBusConnectionString is configured. Set one to enable messaging.");

                    cfg.Host(connectionString);
                    cfg.ConfigureEndpoints(ctx);
                });
            }
        });

        return services;
    }
}
