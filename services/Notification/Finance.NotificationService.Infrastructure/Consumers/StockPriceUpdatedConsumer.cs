using Finance.Contracts.Events;
using Finance.NotificationService.Infrastructure.Hubs;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Finance.NotificationService.Infrastructure.Consumers;

public class StockPriceUpdatedConsumer : IConsumer<StockPriceUpdated>
{
    private readonly IHubContext<StockPriceHub> _hub;
    private readonly ILogger<StockPriceUpdatedConsumer> _logger;

    public StockPriceUpdatedConsumer(IHubContext<StockPriceHub> hub, ILogger<StockPriceUpdatedConsumer> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<StockPriceUpdated> context)
    {
        var msg = context.Message;
        await _hub.Clients.Group(msg.Ticker)
            .SendAsync("ReceiveStockPrice", msg.Ticker, msg.Price, msg.UnixTimestamp, context.CancellationToken);

        _logger.LogInformation("Broadcast price update: {Ticker} = {Price}", msg.Ticker, msg.Price);
    }
}
