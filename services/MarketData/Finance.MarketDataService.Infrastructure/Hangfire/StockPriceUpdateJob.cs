using Finance.Contracts.Events;
using Finance.MarketDataService.Application.Contracts;
using Finance.MarketDataService.Infrastructure.Constants;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Finance.MarketDataService.Infrastructure.Hangfire;

public class StockPriceUpdateJob : IStockPriceUpdateJob
{
    private readonly IRedisCacheService _cache;
    private readonly IStockQuoteService _stockQuote;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<StockPriceUpdateJob> _logger;

    public StockPriceUpdateJob(
        IRedisCacheService cache,
        IStockQuoteService stockQuote,
        IPublishEndpoint publishEndpoint,
        ILogger<StockPriceUpdateJob> logger)
    {
        _cache = cache;
        _stockQuote = stockQuote;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task UpdateStockPricesAsync()
    {
        var tickers = await _cache.GetActiveTickersAsync();
        if (!tickers.Any()) return;

        foreach (var ticker in tickers)
        {
            try
            {
                var response = await _stockQuote.FetchStockQuoteAsync(ticker);
                var result = response?.Chart?.Result?.FirstOrDefault();
                if (result is null) continue;

                var price = result.Meta.RegularMarketPrice;
                if (price <= 0) continue;

                // Cache price for background service reads
                await _cache.SetCacheAsync($"{MarketDataConstants.Redis.PriceCacheKeyPrefix}{ticker}", price, MarketDataConstants.Redis.PriceCacheTtlMinutes);

                // Notify Alert Service and Notification Service
                await _publishEndpoint.Publish(new StockPriceUpdated(
                    ticker,
                    price,
                    DateTimeOffset.UtcNow.ToUnixTimeSeconds()));

                _logger.LogInformation("Updated price for {Ticker}: {Price}", ticker, price);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update price for {Ticker}", ticker);
            }
        }
    }
}
