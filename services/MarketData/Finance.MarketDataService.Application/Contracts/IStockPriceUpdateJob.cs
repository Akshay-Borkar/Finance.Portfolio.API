namespace Finance.MarketDataService.Application.Contracts;

public interface IStockPriceUpdateJob
{
    Task UpdateStockPricesAsync();
}
