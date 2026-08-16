using Finance.SharedKernel.Auth.Middleware;
using Finance.AlertService.Application;
using Finance.AlertService.Infrastructure;
using Finance.AlertService.Persistence;
using Finance.AlertService.Persistence.DatabaseContext;
using Finance.SharedKernel.Auth;
using Finance.SharedKernel.Logging;
using Finance.SharedKernel.Logging.Middleware;
using Finance.SharedKernel.Telemetry;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddSharedLogging("alert");
builder.AddSharedTelemetry("alert");

builder.Configuration.AddUserSecrets<Program>();

builder.Services.AddApplicationServices();
builder.Services.AddPersistenceServices(builder.Configuration);
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddSharedJwtAuthentication(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AlertDbContext>();
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
