using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Finance.SharedKernel.Auth;

public static class JwtAuthenticationExtensions
{
    public static IServiceCollection AddSharedJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var key = configuration[AuthConstants.Config.JwtKey]
            ?? throw new InvalidOperationException("JwtSettings:Key is not configured.");

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                    ValidateIssuer = true,
                    ValidIssuer = configuration[AuthConstants.Config.JwtIssuer],
                    ValidateAudience = true,
                    ValidAudience = configuration[AuthConstants.Config.JwtAudience],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                // Support JWT in SignalR query string
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = ctx =>
                    {
                        var accessToken = ctx.Request.Query[AuthConstants.SignalR.AccessTokenQueryParam];
                        var path = ctx.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments(AuthConstants.SignalR.HubPathPrefix))
                            ctx.Token = accessToken;
                        return Task.CompletedTask;
                    }
                };
            });

        return services;
    }
}
