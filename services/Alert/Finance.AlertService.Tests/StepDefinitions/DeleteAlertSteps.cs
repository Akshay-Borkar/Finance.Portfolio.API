using Finance.AlertService.Application.Contracts.Persistence;
using Finance.AlertService.Application.Features.Alerts.DeleteAlert;
using Finance.AlertService.Domain.Entities;
using Finance.AlertService.Tests.Support;
using FluentAssertions;
using Moq;
using Reqnroll;

namespace Finance.AlertService.Tests.StepDefinitions;

[Binding]
public class DeleteAlertSteps
{
    private readonly AlertTestContext _ctx;
    private readonly Mock<IStockPriceAlertRepository> _alertRepo = new();

    public DeleteAlertSteps(AlertTestContext ctx) => _ctx = ctx;

    [Given(@"an alert with id ""(.*)"" belonging to the current user")]
    public void GivenAlertBelongingToCurrentUser(string alertId)
    {
        var id = Guid.Parse(alertId);
        _alertRepo.Setup(r => r.GetByIdAsync(id))
            .ReturnsAsync(new StockPriceAlert { Id = id, UserId = _ctx.UserId });
        _alertRepo.Setup(r => r.DeleteAsync(It.IsAny<StockPriceAlert>())).Returns(Task.CompletedTask);
    }

    [Given(@"no alert exists with id ""(.*)""")]
    public void GivenNoAlertExists(string alertId) =>
        _alertRepo.Setup(r => r.GetByIdAsync(Guid.Parse(alertId))).ReturnsAsync((StockPriceAlert?)null);

    [Given(@"an alert with id ""(.*)"" belonging to a different user")]
    public void GivenAlertBelongingToDifferentUser(string alertId) =>
        _alertRepo.Setup(r => r.GetByIdAsync(Guid.Parse(alertId)))
            .ReturnsAsync(new StockPriceAlert { Id = Guid.Parse(alertId), UserId = Guid.NewGuid() });

    [When(@"I delete alert ""(.*)""")]
    public async Task WhenIDeleteAlert(string alertId)
    {
        var handler = new DeleteAlertCommandHandler(_alertRepo.Object);
        await handler.Handle(new DeleteAlertCommand(Guid.Parse(alertId), _ctx.UserId), CancellationToken.None);
    }

    [Then(@"the alert should be deleted from the repository")]
    public void ThenAlertDeleted() =>
        _alertRepo.Verify(r => r.DeleteAsync(It.IsAny<StockPriceAlert>()), Times.Once);

    [Then(@"no alert should be deleted")]
    public void ThenNoAlertDeleted() =>
        _alertRepo.Verify(r => r.DeleteAsync(It.IsAny<StockPriceAlert>()), Times.Never);
}
