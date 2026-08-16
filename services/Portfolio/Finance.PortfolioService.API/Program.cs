using Finance.SharedKernel.Auth;
using Finance.SharedKernel.Auth.Middleware;
using Finance.PortfolioService.Application;
using Finance.PortfolioService.Infrastructure;
using Finance.PortfolioService.Persistence;
using Finance.PortfolioService.Persistence.DatabaseContext;
using Finance.SharedKernel.Logging;
using Finance.SharedKernel.Logging.Middleware;
using Finance.SharedKernel.Telemetry;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddSharedLogging("portfolio");
builder.AddSharedTelemetry("portfolio");

builder.Services.AddApplicationServices();
builder.Services.AddPersistenceServices(builder.Configuration);
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddSharedJwtAuthentication(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// Auto-migrate PortfolioDbContext on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PortfolioDbContext>();
    db.Database.Migrate();
}

app.MapOpenApi();
app.MapScalarApiReference();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
