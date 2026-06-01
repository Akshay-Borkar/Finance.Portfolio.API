using System.Security.Claims;
using Finance.Gateway.Constants;
using Finance.SharedKernel.Auth;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = builder.Configuration[AuthConstants.Config.AppInsightsConnectionString];
});

builder.Services.AddSharedJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorization();

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection(GatewayConstants.Config.ReverseProxySection));

builder.Services.AddCors(options =>
{
    options.AddPolicy(AuthConstants.Cors.PolicyName, policy =>
        policy.WithOrigins(AuthConstants.Cors.AllowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

var app = builder.Build();

app.UseCors(AuthConstants.Cors.PolicyName);
app.UseAuthentication();
app.UseAuthorization();

// Inject user identity as forwarded headers so downstream services can read them
// without re-validating JWT (they still do their own validation as defense-in-depth)
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        var userId = context.User.FindFirstValue(AuthConstants.Claims.UserId);
        var email = context.User.FindFirstValue(ClaimTypes.Email)
                 ?? context.User.FindFirstValue(AuthConstants.Claims.Email);

        if (userId is not null)
            context.Request.Headers[AuthConstants.Headers.UserId] = userId;
        if (email is not null)
            context.Request.Headers[AuthConstants.Headers.UserEmail] = email;
    }
    await next();
});

app.MapReverseProxy();
app.MapGet("/health", () => Results.Ok(new { service = GatewayConstants.ServiceName, status = "healthy" }));

app.Run();
