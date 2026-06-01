using Finance.IdentityService.API.Middleware;
using Finance.IdentityService.Infrastructure;
using Finance.IdentityService.Infrastructure.Constants;
using Finance.IdentityService.Persistence;
using Finance.IdentityService.Persistence.DbContext;
using Finance.SharedKernel.Auth;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = builder.Configuration[AuthConstants.Config.AppInsightsConnectionString];
});

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
app.UseCors(AuthConstants.Cors.PolicyName);
app.UseMiddleware<ExceptionMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { service = IdentityConstants.ServiceName, status = "healthy" }));

app.Run();
