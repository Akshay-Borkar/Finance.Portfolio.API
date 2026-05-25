using Finance.NotificationService.Infrastructure.Hubs;
using Finance.NotificationService.Infrastructure;
using Finance.SharedKernel.Auth;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
});

builder.Configuration.AddUserSecrets<Program>();

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

app.MapHub<StockPriceHub>("/hubs/stockprice");

app.Run();
