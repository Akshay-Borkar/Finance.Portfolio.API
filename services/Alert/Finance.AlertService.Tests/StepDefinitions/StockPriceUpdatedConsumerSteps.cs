using Finance.AlertService.Application.Contracts.Persistence;
using Finance.AlertService.Domain.Entities;
using Finance.AlertService.Infrastructure.Consumers;
using Finance.Contracts.Events;
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using Reqnroll;

namespace Finance.AlertService.Tests.StepDefinitions;

[Binding]
public class StockPriceUpdatedConsumerSteps
{
    private readonly Mock<IStockPriceAlertRepository> _alertRepo = new();
    private readonly Mock<IPublishEndpoint> _publisher = new();
    private readonly Mock<ILogger<StockPriceUpdatedConsumer>> _logger = new();

    private readonly List<StockPriceAlert> _activeAlerts = [];
    private string _currentTicker = string.Empty;

    [Given(@"an active ""(.*)"" alert for ticker ""(.*)"" with target price (.*)")]
    public void GivenAnActiveAlert(string condition, string ticker, decimal targetPrice)
    {
        _currentTicker = ticker;
        _activeAlerts.Add(new StockPriceAlert
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Ticker = ticker,
            UserEmail = $"user{_activeAlerts.Count}@test.com",
            Condition = Enum.Parse<AlertCondition>(condition),
            TargetPrice = targetPrice,
            IsTriggered = false
        });
    }

    [Given(@"there are no active alerts for ticker ""(.*)""")]
    public void GivenNoActiveAlerts(string ticker)
    {
        _currentTicker = ticker;
        _activeAlerts.Clear();
    }

    [When(@"a price update message arrives for ""(.*)"" with price (.*)")]
    public async Task WhenPriceUpdateArrives(string ticker, decimal price)
    {
        _alertRepo
            .Setup(r => r.GetActiveAlertsByTickerAsync(ticker))
            .ReturnsAsync(_activeAlerts.Where(a => a.Ticker == ticker).ToList());
        _alertRepo.Setup(r => r.UpdateAsync(It.IsAny<StockPriceAlert>())).Returns(Task.CompletedTask);
        _publisher.Setup(p => p.Publish(It.IsAny<AlertTriggered>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var ctx = new Mock<ConsumeContext<StockPriceUpdated>>();
        ctx.Setup(c => c.Message).Returns(new StockPriceUpdated(ticker, price, DateTimeOffset.UtcNow.ToUnixTimeSeconds()));
        ctx.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        var consumer = new StockPriceUpdatedConsumer(_alertRepo.Object, _publisher.Object, _logger.Object);
        await consumer.Consume(ctx.Object);
    }

    [Then(@"the alert should be marked as triggered")]
    public void ThenAlertTriggered() =>
        _activeAlerts.Should().Contain(a => a.IsTriggered);

    [Then(@"the alert should not be triggered")]
    public void ThenAlertNotTriggered() =>
        _activeAlerts.Should().NotContain(a => a.IsTriggered);

    [Then(@"an AlertTriggered event should be published with direction ""(.*)""")]
    public void ThenAlertTriggeredPublished(string direction) =>
        _publisher.Verify(p => p.Publish(
            It.Is<AlertTriggered>(e => e.Direction == direction),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);

    [Then(@"the published event should carry the current price (.*)")]
    public void ThenPublishedEventPrice(decimal price) =>
        _publisher.Verify(p => p.Publish(
            It.Is<AlertTriggered>(e => e.CurrentPrice == price),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);

    [Then(@"no AlertTriggered event should be published")]
    public void ThenNoAlertTriggeredPublished() =>
        _publisher.Verify(p => p.Publish(It.IsAny<AlertTriggered>(), It.IsAny<CancellationToken>()), Times.Never);

    [Then(@"(\d+) alerts should be triggered")]
    public void ThenAlertsTriggered(int count) =>
        _activeAlerts.Count(a => a.IsTriggered).Should().Be(count);

    [Then(@"(\d+) AlertTriggered events should be published")]
    public void ThenAlertTriggeredCount(int count) =>
        _publisher.Verify(p => p.Publish(It.IsAny<AlertTriggered>(), It.IsAny<CancellationToken>()), Times.Exactly(count));
}
