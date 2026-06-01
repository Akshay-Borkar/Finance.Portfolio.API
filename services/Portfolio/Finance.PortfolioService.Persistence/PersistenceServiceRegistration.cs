using Finance.PortfolioService.Application.Contracts.Persistence;
using Finance.PortfolioService.Persistence.DatabaseContext;
using Finance.PortfolioService.Persistence.Repositories;
using Finance.SharedKernel.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Finance.PortfolioService.Persistence;

public static class PersistenceServiceRegistration
{
    public static IServiceCollection AddPersistenceServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<PortfolioDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("PortfolioDb"),
                sql => sql.EnableRetryOnFailure(maxRetryCount: AuthConstants.Database.RetryMaxCount, maxRetryDelay: TimeSpan.FromSeconds(AuthConstants.Database.RetryDelaySeconds), errorNumbersToAdd: null)));

        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        services.AddScoped<IStockRepository, StockRepository>();
        services.AddScoped<IStockSectorRepository, StockSectorRepository>();
        services.AddScoped<IInvestmentRepository, InvestmentRepository>();

        return services;
    }
}
