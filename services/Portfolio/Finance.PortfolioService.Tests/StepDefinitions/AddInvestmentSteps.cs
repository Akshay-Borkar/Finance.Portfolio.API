using Finance.PortfolioService.Application.Contracts.Persistence;
using Finance.PortfolioService.Application.Features.Portfolio.Commands.AddInvestment;
using Finance.PortfolioService.Domain.Entities;
using Finance.PortfolioService.Tests.Support;
using FluentAssertions;
using Moq;
using Reqnroll;

namespace Finance.PortfolioService.Tests.StepDefinitions;

[Binding]
public class AddInvestmentSteps
{
    private readonly PortfolioTestContext _ctx;
    private readonly Mock<IInvestmentRepository> _investmentRepo = new();
    private readonly Mock<IStockRepository> _stockRepo = new();

    private Guid _targetStockId;
    private Guid _result;
    private Investment? _capturedInvestment;
    private Exception? _thrownException;

    public AddInvestmentSteps(PortfolioTestContext ctx) => _ctx = ctx;

    [Given(@"a stock with id ""(.*)"" owned by the current user")]
    public void GivenStockOwnedByCurrentUser(string stockId)
    {
        _targetStockId = Guid.Parse(stockId);
        _stockRepo
            .Setup(r => r.GetByIdAsync(_targetStockId))
            .ReturnsAsync(new Stock { Id = _targetStockId, UserId = _ctx.UserId });
    }

    [Given(@"no stock exists with id ""(.*)""")]
    public void GivenNoStockExists(string stockId)
    {
        _targetStockId = Guid.Parse(stockId);
        _stockRepo.Setup(r => r.GetByIdAsync(_targetStockId)).ReturnsAsync((Stock?)null);
    }

    [Given(@"a stock with id ""(.*)"" owned by a different user")]
    public void GivenStockOwnedByDifferentUser(string stockId)
    {
        _targetStockId = Guid.Parse(stockId);
        _stockRepo
            .Setup(r => r.GetByIdAsync(_targetStockId))
            .ReturnsAsync(new Stock { Id = _targetStockId, UserId = Guid.NewGuid() });
    }

    [When(@"I add an investment of (.*) at a buying price of (.*) on ""(.*)""")]
    public async Task WhenIAddAnInvestment(decimal amount, double buyingPrice, DateTime date)
    {
        _investmentRepo
            .Setup(r => r.CreateAsync(It.IsAny<Investment>()))
            .Callback<Investment>(i => _capturedInvestment = i)
            .Returns(Task.CompletedTask);

        var handler = new AddInvestmentCommandHandler(_investmentRepo.Object, _stockRepo.Object);
        _result = await handler.Handle(new AddInvestmentCommand
        {
            StockId = _targetStockId,
            UserId = _ctx.UserId,
            InvestedAmount = amount,
            BuyingPrice = buyingPrice,
            InvestmentDate = date
        }, CancellationToken.None);
    }

    [When(@"I try to add an investment of (.*) at a buying price of (.*) to that stock")]
    public async Task WhenITryToAddAnInvestment(decimal amount, double buyingPrice)
    {
        var handler = new AddInvestmentCommandHandler(_investmentRepo.Object, _stockRepo.Object);
        try
        {
            await handler.Handle(new AddInvestmentCommand
            {
                StockId = _targetStockId,
                UserId = _ctx.UserId,
                InvestedAmount = amount,
                BuyingPrice = buyingPrice,
                InvestmentDate = DateTime.UtcNow
            }, CancellationToken.None);
        }
        catch (Exception ex) { _thrownException = ex; }
    }

    [Then(@"a new investment should be created")]
    public void ThenNewInvestmentCreated() =>
        _capturedInvestment.Should().NotBeNull();

    [Then(@"the investment should have an amount of (.*)")]
    public void ThenInvestmentAmount(decimal amount) =>
        _capturedInvestment!.InvestedAmount.Should().Be(amount);

    [Then(@"the investment should have a buying price of (.*)")]
    public void ThenInvestmentBuyingPrice(double price) =>
        _capturedInvestment!.BuyingPrice.Should().Be(price);

    [Then(@"the returned investment id should not be empty")]
    public void ThenReturnedIdNotEmpty() =>
        _result.Should().NotBe(Guid.Empty);

    [Then(@"an unauthorized access error should be raised")]
    public void ThenUnauthorizedAccessError() =>
        _thrownException.Should().BeOfType<UnauthorizedAccessException>();
}
