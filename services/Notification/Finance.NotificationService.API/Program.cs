using Finance.NotificationService.Infrastructure;
using Finance.NotificationService.Infrastructure.Constants;
using Finance.NotificationService.Infrastructure.Hubs;
using Finance.NotificationService.Persistence;
using Finance.NotificationService.Persistence.DatabaseContext;
using Finance.SharedKernel.Auth;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = builder.Configuration[AuthConstants.Config.AppInsightsConnectionString];
});

builder.Configuration.AddUserSecrets<Program>();

builder.Services.AddNotificationPersistence(builder.Configuration);
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddSharedJwtAuthentication(builder.Configuration);

var signalR = builder.Services.AddSignalR();

// A single connection string covers both cases: a bare "host:port" for local/docker Redis
// (no auth, no TLS), or a full Azure Cache for Redis connection string (which already embeds
// password/ssl/abortConnect) — no code change needed to switch targets.
var redisConnectionString = builder.Configuration.GetConnectionString("Redis");

if (!string.IsNullOrWhiteSpace(redisConnectionString))
{
    signalR.AddStackExchangeRedis(options =>
    {
        options.ConnectionFactory = async writer =>
        {
            var config = StackExchange.Redis.ConfigurationOptions.Parse(redisConnectionString);
            config.AbortOnConnectFail = false;
            config.ConnectTimeout = NotificationConstants.Config.RedisConnectTimeoutMs;
            config.SyncTimeout = NotificationConstants.Config.RedisSyncTimeoutMs;

            var connection = await StackExchange.Redis
                .ConnectionMultiplexer.ConnectAsync(config, writer);
            return connection;
        };
    });
}

builder.Services.AddCors(options =>
{
    options.AddPolicy(AuthConstants.Cors.PolicyName, policy =>
        policy.WithOrigins(AuthConstants.Cors.AllowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials());
});

var app = builder.Build();

app.UseCors(AuthConstants.Cors.PolicyName);
app.UseAuthentication();
app.UseAuthorization();

// Auto-migrate NotificationDB on startup.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
    db.Database.Migrate();
}

app.MapHub<StockPriceHub>(NotificationConstants.Hubs.StockPriceRoute);
app.MapHub<PortfolioReviewHub>(NotificationConstants.Hubs.PortfolioReviewRoute);

app.Run();
