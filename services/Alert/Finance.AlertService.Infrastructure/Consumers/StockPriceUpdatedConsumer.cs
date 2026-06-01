using Finance.AlertService.Application.Contracts.Persistence;
using Finance.AlertService.Domain.Entities;
using Finance.AlertService.Infrastructure.Constants;
using Finance.Contracts.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Finance.AlertService.Infrastructure.Consumers;

public class StockPriceUpdatedConsumer : IConsumer<StockPriceUpdated>
{
    private readonly IStockPriceAlertRepository _alertRepository;
    private readonly IPublishEndpoint _publisher;
    private readonly ILogger<StockPriceUpdatedConsumer> _logger;

    public StockPriceUpdatedConsumer(
        IStockPriceAlertRepository alertRepository,
        IPublishEndpoint publisher,
        ILogger<StockPriceUpdatedConsumer> logger)
    {
        _alertRepository = alertRepository;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<StockPriceUpdated> context)
    {
        var msg = context.Message;
        var activeAlerts = await _alertRepository.GetActiveAlertsByTickerAsync(msg.Ticker);

        foreach (var alert in activeAlerts)
        {
            bool triggered = alert.Condition == AlertCondition.Above
                ? msg.Price >= alert.TargetPrice
                : msg.Price <= alert.TargetPrice;

            if (!triggered) continue;

            alert.IsTriggered = true;
            await _alertRepository.UpdateAsync(alert);

            var direction = alert.Condition == AlertCondition.Above ? AlertConstants.Direction.Above : AlertConstants.Direction.Below;

            await _publisher.Publish(new AlertTriggered(
                alert.Id,
                alert.UserId,
                alert.UserEmail,
                alert.Ticker,
                alert.TargetPrice,
                msg.Price,
                direction), context.CancellationToken);

            _logger.LogInformation(
                "Alert {AlertId} triggered for {Ticker}: price {Price} is {Direction} target {Target}",
                alert.Id, alert.Ticker, msg.Price, direction, alert.TargetPrice);
        }
    }
}
