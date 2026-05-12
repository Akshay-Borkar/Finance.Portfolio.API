using Finance.AlertService.Application.Contracts.Persistence;
using Finance.AlertService.Persistence.DatabaseContext;
using Finance.AlertService.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Finance.AlertService.Persistence;

public static class PersistenceServiceRegistration
{
    public static IServiceCollection AddPersistenceServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AlertDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("AlertDb"),
                sql => sql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null)));

        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        services.AddScoped<IStockPriceAlertRepository, StockPriceAlertRepository>();

        return services;
    }
}
