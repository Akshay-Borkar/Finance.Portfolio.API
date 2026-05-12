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
        services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));
        services.AddSingleton<EmailSender>();

        services.AddMassTransit(x =>
        {
            x.AddConsumer<StockPriceUpdatedConsumer>();
            x.AddConsumer<AlertTriggeredConsumer>();

            x.UsingRabbitMq((ctx, cfg) =>
            {
                cfg.Host(
                    configuration["RabbitMq:Host"] ?? "rabbitmq",
                    h =>
                    {
                        h.Username(configuration["RabbitMq:Username"] ?? "guest");
                        h.Password(configuration["RabbitMq:Password"] ?? "guest");
                    });

                cfg.ConfigureEndpoints(ctx);
            });
        });

        return services;
    }
}
