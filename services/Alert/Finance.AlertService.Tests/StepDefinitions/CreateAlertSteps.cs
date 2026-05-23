using Finance.AlertService.Application.Contracts.Persistence;
using Finance.AlertService.Application.Features.Alerts.CreateAlert;
using Finance.AlertService.Domain.Entities;
using Finance.AlertService.Tests.Support;
using FluentAssertions;
using FluentValidation.Results;
using Moq;
using Reqnroll;

namespace Finance.AlertService.Tests.StepDefinitions;

[Binding]
public class CreateAlertSteps
{
    private readonly AlertTestContext _ctx;
    private readonly Mock<IStockPriceAlertRepository> _alertRepo = new();

    private Guid _result;
    private StockPriceAlert? _capturedAlert;
    private Exception? _thrownException;
    private ValidationResult? _validationResult;

    public CreateAlertSteps(AlertTestContext ctx) => _ctx = ctx;

    [When(@"I create an alert for ticker ""(.*)"" with condition ""(.*)"" and target price (.*)")]
    public async Task WhenICreateAnAlert(string ticker, string condition, decimal targetPrice)
    {
        _alertRepo
            .Setup(r => r.CreateAsync(It.IsAny<StockPriceAlert>()))
            .Callback<StockPriceAlert>(a => _capturedAlert = a)
            .Returns(Task.CompletedTask);

        var handler = new CreateAlertCommandHandler(_alertRepo.Object);
        _result = await handler.Handle(new CreateAlertCommand
        {
            UserId = _ctx.UserId,
            Ticker = ticker,
            UserEmail = _ctx.UserEmail,
            Condition = condition,
            TargetPrice = targetPrice
        }, CancellationToken.None);
    }

    [When(@"I try to create an alert for ticker ""(.*)"" with condition ""(.*)"" and target price (.*)")]
    public async Task WhenITryToCreateAnAlert(string ticker, string condition, decimal targetPrice)
    {
        var handler = new CreateAlertCommandHandler(_alertRepo.Object);
        try
        {
            await handler.Handle(new CreateAlertCommand
            {
                UserId = _ctx.UserId,
                Ticker = ticker,
                UserEmail = _ctx.UserEmail,
                Condition = condition,
                TargetPrice = targetPrice
            }, CancellationToken.None);
        }
        catch (Exception ex) { _thrownException = ex; }
    }

    [When(@"I try to submit an alert with ticker ""(.*)"" condition ""(.*)"" target price (.*) and email ""(.*)""")]
    public void WhenITryToSubmitAnAlert(string ticker, string condition, decimal targetPrice, string email)
    {
        var validator = new CreateAlertCommandValidator();
        _validationResult = validator.Validate(new CreateAlertCommand
        {
            UserId = _ctx.UserId == Guid.Empty ? Guid.NewGuid() : _ctx.UserId,
            Ticker = ticker,
            Condition = condition,
            TargetPrice = targetPrice,
            UserEmail = email
        });
    }

    [Then(@"an alert should be saved with ticker ""(.*)""")]
    public void ThenAlertSavedWithTicker(string ticker) =>
        _capturedAlert!.Ticker.Should().Be(ticker);

    [Then(@"the alert condition should be ""(.*)""")]
    public void ThenAlertCondition(string condition) =>
        _capturedAlert!.Condition.ToString().Should().Be(condition);

    [Then(@"the alert target price should be (.*)")]
    public void ThenAlertTargetPrice(decimal price) =>
        _capturedAlert!.TargetPrice.Should().Be(price);

    [Then(@"the alert should not be triggered")]
    public void ThenAlertNotTriggered() =>
        _capturedAlert!.IsTriggered.Should().BeFalse();

    [Then(@"the returned alert id should not be empty")]
    public void ThenReturnedAlertIdNotEmpty() =>
        _result.Should().NotBe(Guid.Empty);

    [Then(@"an argument error should be raised")]
    public void ThenArgumentError() =>
        _thrownException.Should().BeAssignableTo<ArgumentException>();

    [Then(@"the alert submission should be rejected")]
    public void ThenAlertRejected() =>
        _validationResult!.IsValid.Should().BeFalse();

    [Then(@"the validation error should mention ""(.*)""")]
    public void ThenValidationErrorMentions(string field) =>
        _validationResult!.Errors.Should().Contain(e => e.PropertyName == field);
}
