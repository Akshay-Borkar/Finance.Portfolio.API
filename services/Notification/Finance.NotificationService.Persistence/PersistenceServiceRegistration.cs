using Finance.NotificationService.Persistence.DatabaseContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Finance.NotificationService.Persistence;

public static class PersistenceServiceRegistration
{
    public static IServiceCollection AddNotificationPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<NotificationDbContext>(options =>
        {
            var connectionString = configuration["ConnectionStrings:NotificationDb"];
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException(
                    "ConnectionStrings:NotificationDb is not configured.");

            options.UseSqlServer(connectionString);
        });

        return services;
    }
}
