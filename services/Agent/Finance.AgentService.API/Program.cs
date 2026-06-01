using Finance.AgentService.Infrastructure;
using Finance.SharedKernel.Auth;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = builder.Configuration[AuthConstants.Config.AppInsightsConnectionString];
});

builder.Configuration.AddUserSecrets<Program>();

builder.Services.AddControllers();
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

app.UseCors(AuthConstants.Cors.PolicyName);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
