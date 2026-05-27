using Finance.AgentService.Infrastructure;
using Finance.SharedKernel.Auth;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
});

builder.Configuration.AddUserSecrets<Program>();

builder.Services.AddControllers();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddSharedJwtAuthentication(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddPolicy("all", policy =>
        policy.WithOrigins(
                "http://localhost:4200",
                "http://localhost:3000",
                "https://stfinanceportfolio.z19.web.core.windows.net")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials());
});

var app = builder.Build();

app.UseCors("all");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
