using System.Security.Claims;
using Finance.SharedKernel.Auth;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSharedJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorization();

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
        policy.WithOrigins("http://localhost:4200", "http://localhost:3000", "https://stfinanceportfolio.z19.web.core.windows.net")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

var app = builder.Build();

app.UseCors("CorsPolicy");
app.UseAuthentication();
app.UseAuthorization();

// Inject user identity as forwarded headers so downstream services can read them
// without re-validating JWT (they still do their own validation as defense-in-depth)
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        var userId = context.User.FindFirstValue("uid");
        var email = context.User.FindFirstValue(ClaimTypes.Email)
                 ?? context.User.FindFirstValue("email");

        if (userId is not null)
            context.Request.Headers["X-User-Id"] = userId;
        if (email is not null)
            context.Request.Headers["X-User-Email"] = email;
    }
    await next();
});

app.MapReverseProxy();
app.MapGet("/health", () => Results.Ok(new { service = "gateway", status = "healthy" }));

app.Run();
