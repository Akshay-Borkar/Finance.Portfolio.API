using Finance.MarketDataService.API.Protos;
using Finance.PortfolioService.Application.Contracts.MarketData;
using Finance.PortfolioService.Infrastructure.GrpcClients;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Finance.PortfolioService.Infrastructure;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var marketDataGrpcAddress = configuration["MarketData:GrpcAddress"] ?? "http://marketdata-svc:8081";

        services.AddGrpcClient<MarketDataGrpc.MarketDataGrpcClient>(o =>
        {
            o.Address = new Uri(marketDataGrpcAddress);
        });

        services.AddScoped<IMarketDataGrpcClient, MarketDataGrpcClient>();

        services.AddMassTransit(x =>
        {
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
            // });

            // Azure Service Bus configuration
            x.UsingAzureServiceBus((ctx, cfg) =>
            {
                cfg.Host(configuration["ServiceBus__ConnectionString"]);
                cfg.ConfigureEndpoints(ctx);
            });
        });

        return services;
    }
}
