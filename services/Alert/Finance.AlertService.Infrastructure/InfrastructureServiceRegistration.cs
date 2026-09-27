using Finance.AlertService.Infrastructure.Consumers;
using Finance.SharedKernel.Messaging;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Finance.AlertService.Infrastructure;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSharedMessaging(configuration, x => x.AddConsumer<StockPriceUpdatedConsumer>());

        return services;
    }
}
