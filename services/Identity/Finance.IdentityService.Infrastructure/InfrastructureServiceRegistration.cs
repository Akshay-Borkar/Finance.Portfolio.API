using Finance.IdentityService.Application.Contracts;
using Finance.IdentityService.Application.Models;
using Finance.IdentityService.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Finance.IdentityService.Infrastructure;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddIdentityInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));
        services.AddTransient<IAuthService, AuthService>();
        services.AddHttpContextAccessor();

        return services;
    }
}
