using Finance.Contracts.Events;
using Finance.MarketDataService.Application.Contracts;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Finance.MarketDataService.Infrastructure.Consumers;

public class StockRemovedConsumer : IConsumer<StockRemoved>
{
    private readonly IRedisCacheService _cache;
    private readonly ILogger<StockRemovedConsumer> _logger;

    public StockRemovedConsumer(IRedisCacheService cache, ILogger<StockRemovedConsumer> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<StockRemoved> context)
    {
        var ticker = context.Message.Ticker;
        await _cache.RemoveActiveTickerAsync(ticker);
        _logger.LogInformation("Unsubscribed price tracking for {Ticker}", ticker);
    }
}
