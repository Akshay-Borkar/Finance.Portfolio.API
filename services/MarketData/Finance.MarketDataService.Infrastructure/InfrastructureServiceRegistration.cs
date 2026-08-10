using Finance.MarketDataService.Application.Contracts;
using Finance.MarketDataService.Infrastructure.Constants;
using Finance.MarketDataService.Infrastructure.Consumers;
using Finance.MarketDataService.Infrastructure.Hangfire;
using Finance.MarketDataService.Infrastructure.Redis;
using Finance.MarketDataService.Infrastructure.Services;
using Finance.SharedKernel.Logging.Messaging;
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
        // A single connection string covers both cases: a bare "host:port" for local/docker
        // Redis (no auth, no TLS), or a full Azure Cache for Redis connection string (which
        // already embeds password/ssl/abortConnect) — no code change needed to switch targets.
        IConnectionMultiplexer? redis = null;

        var redisConnectionString = configuration.GetConnectionString("Redis");

        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            try
            {
                var redisConfig = ConfigurationOptions.Parse(redisConnectionString);
                redisConfig.AbortOnConnectFail = false;
                redisConfig.ConnectTimeout = MarketDataConstants.Redis.ConnectTimeoutMs;

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

            // Transport is selected at startup based on configuration: RabbitMq:Host wins when
            // present (local/dev via docker-compose), otherwise falls back to Azure Service Bus
            // (staging/prod). Both branches stay wired so switching is a config change, not a code change.
            var rabbitMqHost = configuration["RabbitMq:Host"];
            if (!string.IsNullOrWhiteSpace(rabbitMqHost))
            {
                // RabbitMQ configuration
                x.UsingRabbitMq((ctx, cfg) =>
                {
                    cfg.Host(rabbitMqHost, "/", h =>
                    {
                        h.Username(configuration["RabbitMq:Username"] ?? "guest");
                        h.Password(configuration["RabbitMq:Password"] ?? "guest");
                    });
                    cfg.UseCorrelationLogging(ctx);
                    cfg.ConfigureEndpoints(ctx);
                });
            }
            else
            {
                // Azure Service Bus configuration
                x.UsingAzureServiceBus((ctx, cfg) =>
                {
                    var connectionString = configuration[MarketDataConstants.Config.ServiceBusConnectionString];
                    if (string.IsNullOrWhiteSpace(connectionString))
                        throw new InvalidOperationException(
                            "Neither RabbitMq:Host nor ServiceBusConnectionString is configured. Set one to enable messaging.");

                    cfg.Host(connectionString);
                    cfg.UseCorrelationLogging(ctx);
                    cfg.ConfigureEndpoints(ctx);
                });
            }
        });

        return services;
    }
}
