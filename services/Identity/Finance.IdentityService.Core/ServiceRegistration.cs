using Finance.IdentityService.Core.Entities;
using Finance.IdentityService.Core.Models;
using Finance.IdentityService.Core.Persistence;
using Finance.IdentityService.Core.Services;
using Finance.SharedKernel.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using IdentityConstants = Finance.IdentityService.Core.Constants.IdentityConstants;

namespace Finance.IdentityService.Core;

public static class ServiceRegistration
{
    /// <summary>
    /// Registers everything this service owns — the SQL Server-backed <see cref="IdentityDbContext"/>,
    /// ASP.NET Identity on top of it, and the services that mint this app's own local JWTs.
    /// </summary>
    /// <remarks>
    /// Deliberately not named <c>AddIdentityCore</c>: ASP.NET Identity already ships an
    /// extension method with that exact name on <c>IServiceCollection</c>, and having both in
    /// scope here would be an ambiguous-call trap at every call site.
    /// </remarks>
    public static IServiceCollection AddIdentityServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<IdentityDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString(IdentityConstants.Config.DbConnectionName),
                sql => sql.EnableRetryOnFailure(
                    maxRetryCount: AuthConstants.Database.RetryMaxCount,
                    maxRetryDelay: TimeSpan.FromSeconds(AuthConstants.Database.RetryDelaySeconds),
                    errorNumbersToAdd: null)));

        services.AddIdentity<ApplicationUser, IdentityRole>()
            .AddEntityFrameworkStores<IdentityDbContext>()
            .AddDefaultTokenProviders();

        services.Configure<JwtSettings>(configuration.GetSection(IdentityConstants.Config.JwtSettingsSection));
        services.AddTransient<IJwtTokenFactory, JwtTokenFactory>();
        services.AddTransient<IAuthService, AuthService>();
        services.AddTransient<IExternalAuthService, ExternalAuthService>();
        services.AddHttpContextAccessor();

        return services;
    }
}
