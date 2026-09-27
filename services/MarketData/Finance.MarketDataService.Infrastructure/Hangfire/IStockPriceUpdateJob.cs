namespace Finance.MarketDataService.Infrastructure.Hangfire;

public interface IStockPriceUpdateJob
{
    Task UpdateStockPricesAsync();
}
