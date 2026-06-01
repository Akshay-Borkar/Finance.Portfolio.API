using Finance.MarketDataService.Application.Contracts;
using Finance.MarketDataService.Infrastructure.Constants;
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
        IConnectionMultiplexer? redis = null;

        var redisEndpoint = configuration[MarketDataConstants.Config.RedisEndpoint];
        var redisPassword = configuration[MarketDataConstants.Config.RedisPassword];

        if (!string.IsNullOrEmpty(redisEndpoint) && !string.IsNullOrEmpty(redisPassword))
        {
            var redisConfig = new ConfigurationOptions
            {
                AbortOnConnectFail = false,
                Ssl = true,
                Password = redisPassword,
                ConnectTimeout = MarketDataConstants.Redis.ConnectTimeoutMs
            };
            redisConfig.EndPoints.Add(redisEndpoint);

            try
            {
                redis = ConnectionMultiplexer.Connect(redisConfig);
            }
            catch { /* fall back to in-memory below */ }
        }

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
                var connectionString = configuration[MarketDataConstants.Config.ServiceBusConnectionString];
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
