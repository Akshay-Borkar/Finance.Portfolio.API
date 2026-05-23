using Finance.AlertService.Application.Common;
using Finance.AlertService.Application.Contracts.Persistence;
using Finance.AlertService.Application.Features.Alerts;
using Finance.AlertService.Application.Features.Alerts.GetUserAlerts;
using Finance.AlertService.Domain.Entities;
using Finance.AlertService.Tests.Support;
using FluentAssertions;
using Moq;
using Reqnroll;

namespace Finance.AlertService.Tests.StepDefinitions;

[Binding]
public class GetUserAlertsSteps
{
    private readonly AlertTestContext _ctx;
    private readonly Mock<IStockPriceAlertRepository> _alertRepo = new();

    private PagedResult<AlertDto> _result = new();

    public GetUserAlertsSteps(AlertTestContext ctx) => _ctx = ctx;

    [Given(@"the user has (\d+) alerts in the repository")]
    public void GivenUserHasAlerts(int count)
    {
        var alerts = Enumerable.Range(0, count).Select(i => new StockPriceAlert
        {
            Id = Guid.NewGuid(), UserId = _ctx.UserId, Ticker = $"TICK{i}",
            Condition = AlertCondition.Above, TargetPrice = 100m + i
        }).ToList();

        _alertRepo
            .Setup(r => r.GetAlertsByUserIdPagedAsync(_ctx.UserId, It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync((alerts, count));
    }

    [Given(@"the user has an alert for ""(.*)"" with condition ""(.*)"" target (.*) that is triggered")]
    public void GivenUserHasSpecificAlert(string ticker, string condition, decimal target)
    {
        var alert = new StockPriceAlert
        {
            Id = Guid.NewGuid(), UserId = _ctx.UserId, Ticker = ticker,
            Condition = Enum.Parse<AlertCondition>(condition), TargetPrice = target, IsTriggered = true
        };
        _alertRepo
            .Setup(r => r.GetAlertsByUserIdPagedAsync(_ctx.UserId, It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(([alert], 1));
    }

    [When(@"I request alerts page (\d+) with page size (\d+)")]
    public async Task WhenIRequestAlerts(int page, int pageSize)
    {
        var handler = new GetUserAlertsQueryHandler(_alertRepo.Object);
        _result = await handler.Handle(new GetUserAlertsQuery(_ctx.UserId, page, pageSize), CancellationToken.None);
    }

    [Then(@"the result should contain (\d+) alerts")]
    public void ThenResultContainsAlerts(int count) =>
        _result.Items.Should().HaveCount(count);

    [Then(@"the total count should be (\d+)")]
    public void ThenTotalCount(int count) =>
        _result.TotalCount.Should().Be(count);

    [Then(@"the repository should be queried with page (\d+) and page size (\d+)")]
    public void ThenRepositoryQueriedWithPagination(int page, int pageSize) =>
        _alertRepo.Verify(r => r.GetAlertsByUserIdPagedAsync(_ctx.UserId, page, pageSize), Times.Once);

    [Then(@"the first alert in the result should have ticker ""(.*)""")]
    public void ThenFirstAlertTicker(string ticker) =>
        _result.Items[0].Ticker.Should().Be(ticker);

    [Then(@"the first alert condition should be ""(.*)""")]
    public void ThenFirstAlertCondition(string condition) =>
        _result.Items[0].Condition.Should().Be(condition);

    [Then(@"the first alert target price should be (.*)")]
    public void ThenFirstAlertTargetPrice(decimal price) =>
        _result.Items[0].TargetPrice.Should().Be(price);

    [Then(@"the first alert should be marked as triggered")]
    public void ThenFirstAlertTriggered() =>
        _result.Items[0].IsTriggered.Should().BeTrue();
}
