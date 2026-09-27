using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Finance.SharedKernel.Messaging;

public static class MessagingExtensions
{
    /// <summary>
    /// Registers MassTransit with correlation-id logging and the transport chosen at startup from
    /// configuration: <c>RabbitMq:Host</c> wins when present (local/dev via docker-compose),
    /// otherwise Azure Service Bus (staging/prod). Both branches stay wired so switching targets
    /// is a config change, not a code change.
    /// </summary>
    /// <param name="configureBus">
    /// Consumer registrations and any other bus-level setup, e.g. <c>x => x.AddConsumer&lt;FooConsumer&gt;()</c>.
    /// </param>
    /// <param name="configureServiceBusEndpoints">
    /// Replaces the default <c>ConfigureEndpoints</c> on the Azure Service Bus branch only. Pass this
    /// when a service needs explicit <c>SubscriptionEndpoint</c> naming: MassTransit otherwise derives
    /// the subscription name from the consumer class in kebab-case, which appends "-consumer" and so
    /// does not match the topic/subscription pairs provisioned on Azure. The RabbitMQ branch always
    /// uses convention-based naming, where that suffix is harmless.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// Neither <c>RabbitMq:Host</c> nor <c>ServiceBusConnectionString</c> is configured.
    /// </exception>
    public static IServiceCollection AddSharedMessaging(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<IBusRegistrationConfigurator>? configureBus = null,
        Action<IServiceBusBusFactoryConfigurator, IBusRegistrationContext>? configureServiceBusEndpoints = null)
    {
        services.AddMassTransit(x =>
        {
            configureBus?.Invoke(x);

            var rabbitMqHost = configuration[MessagingConstants.Config.RabbitMqHost];

            if (!string.IsNullOrWhiteSpace(rabbitMqHost))
            {
                x.UsingRabbitMq((ctx, cfg) =>
                {
                    cfg.Host(rabbitMqHost, h =>
                    {
                        h.Username(configuration[MessagingConstants.Config.RabbitMqUsername]
                            ?? MessagingConstants.DefaultRabbitMqCredential);
                        h.Password(configuration[MessagingConstants.Config.RabbitMqPassword]
                            ?? MessagingConstants.DefaultRabbitMqCredential);
                    });

                    cfg.UseCorrelationLogging(ctx);
                    cfg.ConfigureEndpoints(ctx);
                });
            }
            else
            {
                x.UsingAzureServiceBus((ctx, cfg) =>
                {
                    var connectionString = configuration[MessagingConstants.Config.ServiceBusConnectionString];
                    if (string.IsNullOrWhiteSpace(connectionString))
                        throw new InvalidOperationException(
                            "Neither RabbitMq:Host nor ServiceBusConnectionString is configured. Set one to enable messaging.");

                    cfg.Host(connectionString);
                    cfg.UseCorrelationLogging(ctx);

                    if (configureServiceBusEndpoints is not null)
                        configureServiceBusEndpoints(cfg, ctx);
                    else
                        cfg.ConfigureEndpoints(ctx);
                });
            }
        });

        return services;
    }
}
