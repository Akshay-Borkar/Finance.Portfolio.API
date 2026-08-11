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

        var auth = services.AddAuthentication(options =>
        {
            // The locally-issued HMAC JWT stays the default scheme everywhere. Every existing
            // [Authorize] attribute in the codebase is scheme-agnostic, so this is a no-op for
            // all of them — only an endpoint that explicitly opts into
            // AuthConstants.Schemes.EntraExternalId (see Identity's /api/auth/external/login)
            // is ever authenticated against the second scheme below.
            options.DefaultAuthenticateScheme = AuthConstants.Schemes.LocalJwt;
            options.DefaultChallengeScheme = AuthConstants.Schemes.LocalJwt;
        });

        auth.AddJwtBearer(AuthConstants.Schemes.LocalJwt, options =>
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

        // Second scheme: validates raw Microsoft Entra External ID access tokens via the
        // tenant's own JWKS (asymmetric, auto-discovered from Authority — no shared secret).
        // Guarded on Instance/TenantId actually having values (not just the section existing)
        // so shipping an empty "AzureAd": {} placeholder in appsettings — same convention as
        // JwtSettings:Key="" — never registers a scheme with a malformed Authority. Only ever
        // exercised by the one endpoint that requests it explicitly via
        // [Authorize(AuthenticationSchemes = AuthConstants.Schemes.EntraExternalId)].
        var instance = configuration[AuthConstants.Config.AzureAdInstance];
        var tenantId = configuration[AuthConstants.Config.AzureAdTenantId];
        if (!string.IsNullOrWhiteSpace(instance) && !string.IsNullOrWhiteSpace(tenantId))
        {
            auth.AddJwtBearer(AuthConstants.Schemes.EntraExternalId, options =>
            {
                options.Authority = $"{instance.TrimEnd('/')}/{tenantId}/v2.0";
                options.Audience = configuration[AuthConstants.Config.AzureAdAudience];
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    NameClaimType = "name",
                };
            });
        }

        return services;
    }
}
