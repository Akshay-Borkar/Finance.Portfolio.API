using Finance.MarketDataService.Infrastructure.Constants;
using Finance.MarketDataService.Infrastructure.Consumers;
using Finance.MarketDataService.Infrastructure.Hangfire;
using Finance.MarketDataService.Infrastructure.Redis;
using Finance.MarketDataService.Infrastructure.Services;
using Finance.SharedKernel.Messaging;
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
        services.AddSharedMessaging(configuration, x =>
        {
            x.AddConsumer<StockAddedConsumer>();
            x.AddConsumer<StockRemovedConsumer>();
        });

        return services;
    }
}
