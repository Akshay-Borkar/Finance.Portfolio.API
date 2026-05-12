using Finance.NotificationService.Infrastructure.Hubs;
using Finance.NotificationService.Infrastructure;
using Finance.SharedKernel.Auth;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddSharedJwtAuthentication(builder.Configuration);

var redisConnection = builder.Configuration.GetConnectionString("Redis");

var signalR = builder.Services.AddSignalR();
if (!string.IsNullOrEmpty(redisConnection))
    signalR.AddStackExchangeRedis(redisConnection);

builder.Services.AddCors(options =>
{
    options.AddPolicy("all", policy =>
        policy.WithOrigins("http://localhost:4200", "http://localhost:3000")
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
