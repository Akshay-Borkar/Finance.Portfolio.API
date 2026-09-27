using Finance.AlertService.Application.Contracts.Persistence;
using Finance.AlertService.Infrastructure.Constants;
using Finance.AlertService.Infrastructure.Consumers;
using Finance.AlertService.Infrastructure.Persistence;
using Finance.AlertService.Infrastructure.Persistence.Repositories;
using Finance.SharedKernel.Auth;
using Finance.SharedKernel.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Finance.AlertService.Infrastructure;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AlertDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString(AlertConstants.Config.DbConnectionName),
                sql => sql.EnableRetryOnFailure(
                    maxRetryCount: AuthConstants.Database.RetryMaxCount,
                    maxRetryDelay: TimeSpan.FromSeconds(AuthConstants.Database.RetryDelaySeconds),
                    errorNumbersToAdd: null)));

        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        services.AddScoped<IStockPriceAlertRepository, StockPriceAlertRepository>();

        services.AddSharedMessaging(configuration, x => x.AddConsumer<StockPriceUpdatedConsumer>());

        return services;
    }
}
