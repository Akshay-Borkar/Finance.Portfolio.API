using Finance.AgentService.Infrastructure;
using Finance.SharedKernel.Auth;
using Finance.SharedKernel.Logging;
using Finance.SharedKernel.Logging.Middleware;
using Finance.SharedKernel.Telemetry;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddSharedLogging("agent");
builder.AddSharedTelemetry("agent");

builder.Configuration.AddUserSecrets<Program>();

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddSharedJwtAuthentication(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddPolicy(AuthConstants.Cors.PolicyName, policy =>
        policy.WithOrigins(AuthConstants.Cors.AllowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials());
});

var app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference();
app.UseCors(AuthConstants.Cors.PolicyName);
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
