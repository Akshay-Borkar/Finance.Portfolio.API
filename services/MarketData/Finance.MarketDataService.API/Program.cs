using Finance.MarketDataService.API.Controllers;
using Finance.MarketDataService.API.Middleware;
using Finance.MarketDataService.Application.Contracts;
using Finance.MarketDataService.Infrastructure;
using Finance.MarketDataService.Infrastructure.Constants;
using Finance.SharedKernel.Auth;
using Hangfire;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = builder.Configuration[AuthConstants.Config.AppInsightsConnectionString];
});

builder.Configuration.AddUserSecrets<Program>();

builder.Services.AddMarketDataInfrastructure(builder.Configuration);
builder.Services.AddGrpc();
builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
    options.AddPolicy(AuthConstants.Cors.PolicyName, p =>
        p.WithOrigins(AuthConstants.Cors.AllowedOrigins)
         .AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

var app = builder.Build();

// Schedule Hangfire recurring jobs
using (var scope = app.Services.CreateScope())
{
    var jobClient = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
    jobClient.AddOrUpdate<IStockPriceUpdateJob>(
        MarketDataConstants.HangfireJobs.Minutely,
        job => job.UpdateStockPricesAsync(),
        Cron.Minutely());
    jobClient.AddOrUpdate<IStockPriceUpdateJob>(
        MarketDataConstants.HangfireJobs.Hourly,
        job => job.UpdateStockPricesAsync(),
        Cron.Hourly());
    jobClient.AddOrUpdate<IStockPriceUpdateJob>(
        MarketDataConstants.HangfireJobs.Daily,
        job => job.UpdateStockPricesAsync(),
        Cron.Daily());
    jobClient.AddOrUpdate<IStockPriceUpdateJob>(
        MarketDataConstants.HangfireJobs.Weekly,
        job => job.UpdateStockPricesAsync(),
        Cron.Weekly(DayOfWeek.Monday, 9, 0));
}

app.MapOpenApi();
app.UseCors(AuthConstants.Cors.PolicyName);
app.UseMiddleware<ExceptionMiddleware>();

app.UseHangfireDashboard(MarketDataConstants.HangfireJobs.DashboardRoute);

// gRPC endpoint (HTTP/2 — separate port 8081 in docker-compose)
app.MapGrpcService<MarketDataGrpcService>();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { service = MarketDataConstants.ServiceName, status = "healthy" }));
app.MapGet("/", () => "Market Data Service — gRPC on port 8081, REST on port 8080");

app.Run();
