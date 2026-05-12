using Finance.Contracts.Events;
using Finance.MarketDataService.Application.Contracts;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Finance.MarketDataService.Infrastructure.Consumers;

public class StockAddedConsumer : IConsumer<StockAdded>
{
    private readonly IRedisCacheService _cache;
    private readonly ILogger<StockAddedConsumer> _logger;

    public StockAddedConsumer(IRedisCacheService cache, ILogger<StockAddedConsumer> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<StockAdded> context)
    {
        var ticker = context.Message.Ticker;
        await _cache.AddActiveTickerAsync(ticker);
        _logger.LogInformation("Subscribed to price tracking for {Ticker}", ticker);
    }
}
