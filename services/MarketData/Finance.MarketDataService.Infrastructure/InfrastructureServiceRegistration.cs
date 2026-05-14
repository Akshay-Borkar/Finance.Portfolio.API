using Finance.MarketDataService.Application.Contracts;
using Finance.MarketDataService.Infrastructure.Consumers;
using Finance.MarketDataService.Infrastructure.Hangfire;
using Finance.MarketDataService.Infrastructure.Redis;
using Finance.MarketDataService.Infrastructure.Services;
using global::Hangfire;
using global::Hangfire.InMemory;
using global::Hangfire.Redis.StackExchange;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Finance.MarketDataService.Infrastructure;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddMarketDataInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── Redis ─────────────────────────────────────────────────────────────
        var redisConn = configuration.GetConnectionString("Redis") ?? "localhost:6379";
        IConnectionMultiplexer? redis = null;
        try
        {
            var opts = ConfigurationOptions.Parse(redisConn);
            opts.ConnectTimeout = 2000;
            opts.AbortOnConnectFail = false;
            redis = ConnectionMultiplexer.Connect(opts);
        }
        catch { /* fall back to in-memory below */ }

        bool redisAvailable = redis?.IsConnected == true;

        if (redisAvailable)
        {
            services.AddSingleton<IConnectionMultiplexer>(redis!);
            services.AddScoped<IRedisCacheService, RedisCacheService>();
            services.AddHangfire(c => c
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseRedisStorage(redis!, new RedisStorageOptions()));
        }
        else
        {
            services.AddScoped<IRedisCacheService, InMemoryFallbackCacheService>();
            services.AddHangfire(c => c
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseInMemoryStorage());
        }

        services.AddHangfireServer();

        // ── Yahoo Finance / HTTP ───────────────────────────────────────────────
        services.AddHttpClient();
        services.AddScoped<IStockQuoteService, StockQuoteService>();
        services.AddScoped<IStockPriceUpdateJob, StockPriceUpdateJob>();

        // ── MassTransit ───────────────────────────────────────────────────────
        services.AddMassTransit(x =>
        {
            x.AddConsumer<StockAddedConsumer>();
            x.AddConsumer<StockRemovedConsumer>();

            // RabbitMQ configuration
            // x.UsingRabbitMq((ctx, cfg) =>
            // {
            //     cfg.Host(configuration["RabbitMq:Host"] ?? "localhost", "/", h =>
            //     {
            //         h.Username(configuration["RabbitMq:Username"] ?? "guest");
            //         h.Password(configuration["RabbitMq:Password"] ?? "guest");
            //     });
            //     cfg.ConfigureEndpoints(ctx);
            // });

            // Azure Service Bus configuration
            x.UsingAzureServiceBus((ctx, cfg) =>
            {
                cfg.Host(configuration["ServiceBusConnectionString"]);
                cfg.ConfigureEndpoints(ctx);
            });
        });

        return services;
    }
}
