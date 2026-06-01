using Finance.IdentityService.Domain;
using Finance.IdentityService.Persistence.DbContext;
using Finance.SharedKernel.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Finance.IdentityService.Persistence;

public static class PersistenceServiceRegistration
{
    public static IServiceCollection AddIdentityPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<IdentityDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("IdentityDb"),
                sql => sql.EnableRetryOnFailure(
                    maxRetryCount: AuthConstants.Database.RetryMaxCount,
                    maxRetryDelay: TimeSpan.FromSeconds(AuthConstants.Database.RetryDelaySeconds),
                    errorNumbersToAdd: null)));

        services.AddIdentity<ApplicationUser, IdentityRole>()
            .AddEntityFrameworkStores<IdentityDbContext>()
            .AddDefaultTokenProviders();

        return services;
    }
}
