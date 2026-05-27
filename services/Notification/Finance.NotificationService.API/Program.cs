using Finance.NotificationService.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Finance.NotificationService.Infrastructure.Hubs;
using Finance.NotificationService.Persistence;
using Finance.NotificationService.Persistence.DatabaseContext;
using Finance.SharedKernel.Auth;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
});

builder.Configuration.AddUserSecrets<Program>();

builder.Services.AddNotificationPersistence(builder.Configuration);
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddSharedJwtAuthentication(builder.Configuration);

var signalR = builder.Services.AddSignalR();

var redisEndpoint = builder.Configuration["RedisEndpoint"];
var redisPassword = builder.Configuration["RedisPassword"];

if (!string.IsNullOrEmpty(redisEndpoint) && !string.IsNullOrEmpty(redisPassword))
{
    signalR.AddStackExchangeRedis(options =>
    {
        options.ConnectionFactory = async writer =>
        {
            var config = new StackExchange.Redis.ConfigurationOptions
            {
                AbortOnConnectFail = false,
                Ssl = true,
                Password = redisPassword,
                ConnectTimeout = 5000,
                SyncTimeout = 5000
            };
            config.EndPoints.Add(redisEndpoint);
            var connection = await StackExchange.Redis
                .ConnectionMultiplexer.ConnectAsync(config, writer);
            return connection;
        };
    });
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("all", policy =>
        policy.WithOrigins("http://localhost:4200", "http://localhost:3000", "https://stfinanceportfolio.z19.web.core.windows.net")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials());
});

var app = builder.Build();

app.UseCors("all");
app.UseAuthentication();
app.UseAuthorization();

// Auto-migrate NotificationDB on startup.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
    db.Database.Migrate();
}

app.MapHub<StockPriceHub>("/hubs/stockprice");
app.MapHub<PortfolioReviewHub>("/hubs/portfolio-review");

app.Run();
