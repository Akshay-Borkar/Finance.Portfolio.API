using Finance.MarketDataService.API.Controllers;
using Finance.MarketDataService.API.Middleware;
using Finance.MarketDataService.Application.Contracts;
using Finance.MarketDataService.Infrastructure;
using Hangfire;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMarketDataInfrastructure(builder.Configuration);
builder.Services.AddGrpc();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
    options.AddPolicy("CorsPolicy", p =>
        p.WithOrigins("http://localhost:4200", "http://localhost:3000")
         .AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

var app = builder.Build();

// Schedule Hangfire recurring jobs
using (var scope = app.Services.CreateScope())
{
    var jobClient = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
    jobClient.AddOrUpdate<IStockPriceUpdateJob>(
        "UpdateStockPricesMinutely",
        job => job.UpdateStockPricesAsync(),
        Cron.Minutely());
    jobClient.AddOrUpdate<IStockPriceUpdateJob>(
        "UpdateStockPricesHourly",
        job => job.UpdateStockPricesAsync(),
        Cron.Hourly());
    jobClient.AddOrUpdate<IStockPriceUpdateJob>(
        "UpdateStockPricesDaily",
        job => job.UpdateStockPricesAsync(),
        Cron.Daily());
    jobClient.AddOrUpdate<IStockPriceUpdateJob>(
        "UpdateStockPricesWeekly",
        job => job.UpdateStockPricesAsync(),
        Cron.Weekly(DayOfWeek.Monday, 9, 0));
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("CorsPolicy");
app.UseMiddleware<ExceptionMiddleware>();

app.UseHangfireDashboard("/hangfire");

// gRPC endpoint (HTTP/2 — separate port 8081 in docker-compose)
app.MapGrpcService<MarketDataGrpcService>();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { service = "marketdata", status = "healthy" }));
app.MapGet("/", () => "Market Data Service — gRPC on port 8081, REST on port 8080");

app.Run();
