using Finance.SharedKernel.Auth.Middleware;
using Finance.SentimentService.Infrastructure;
using Finance.SentimentService.Infrastructure.Constants;
using Finance.SharedKernel.Auth;
using Finance.SharedKernel.Logging;
using Finance.SharedKernel.Logging.Middleware;
using Finance.SharedKernel.Telemetry;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddSharedLogging(SentimentConstants.ServiceName);
builder.AddSharedTelemetry(SentimentConstants.ServiceName);

builder.Services.AddSentimentInfrastructure();
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

app.MapOpenApi();
app.MapScalarApiReference();
app.UseCors(AuthConstants.Cors.PolicyName);
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { service = SentimentConstants.ServiceName, status = "healthy" }));

app.Run();
