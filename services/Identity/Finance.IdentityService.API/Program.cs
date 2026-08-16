using Finance.SharedKernel.Auth.Middleware;
using Finance.IdentityService.Infrastructure;
using Finance.IdentityService.Infrastructure.Constants;
using Finance.IdentityService.Persistence;
using Finance.IdentityService.Persistence.DbContext;
using Finance.SharedKernel.Auth;
using Finance.SharedKernel.Logging;
using Finance.SharedKernel.Logging.Middleware;
using Finance.SharedKernel.Telemetry;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddSharedLogging(IdentityConstants.ServiceName);
builder.AddSharedTelemetry(IdentityConstants.ServiceName);

builder.Services.AddIdentityPersistence(builder.Configuration);
builder.Services.AddIdentityInfrastructure(builder.Configuration);
builder.Services.AddSharedJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddPolicy(AuthConstants.Cors.PolicyName, policy =>
        policy.WithOrigins(AuthConstants.Cors.AllowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

var app = builder.Build();

// Auto-migrate on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    db.Database.Migrate();
}

app.MapOpenApi();
app.MapScalarApiReference();
app.UseCors(AuthConstants.Cors.PolicyName);
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { service = IdentityConstants.ServiceName, status = "healthy" }));

app.Run();
