using Finance.Contracts.Events;
using Finance.PortfolioService.Application.Contracts.Persistence;
using Finance.PortfolioService.Application.Exceptions;
using Finance.PortfolioService.Application.Features.Portfolio.Commands.DeleteStock;
using Finance.PortfolioService.Domain.Entities;
using Finance.PortfolioService.Tests.Support;
using FluentAssertions;
using MassTransit;
using Moq;
using Reqnroll;

namespace Finance.PortfolioService.Tests.StepDefinitions;

[Binding]
public class DeleteStockSteps
{
    private readonly PortfolioTestContext _ctx;
    private readonly Mock<IStockRepository> _stockRepo = new();
    private readonly Mock<IInvestmentRepository> _investmentRepo = new();
    private readonly Mock<IPublishEndpoint> _publisher = new();

    private Guid _targetStockId;
    private Exception? _thrownException;

    public DeleteStockSteps(PortfolioTestContext ctx) => _ctx = ctx;

    [Given(@"a stock with id ""(.*)"" and ticker ""(.*)"" owned by the current user")]
    public void GivenStockOwnedByCurrentUser(string stockId, string ticker)
    {
        _targetStockId = Guid.Parse(stockId);
        _stockRepo
            .Setup(r => r.GetByIdAsync(_targetStockId))
            .ReturnsAsync(new Stock { Id = _targetStockId, UserId = _ctx.UserId, Ticker = ticker });
    }

    [Given(@"a stock with id ""(.*)"" and ticker ""(.*)"" owned by a different user")]
    public void GivenStockOwnedByDifferentUser(string stockId, string ticker)
    {
        _targetStockId = Guid.Parse(stockId);
        _stockRepo
            .Setup(r => r.GetByIdAsync(_targetStockId))
            .ReturnsAsync(new Stock { Id = _targetStockId, UserId = Guid.NewGuid(), Ticker = ticker });
    }

    [Given(@"no stock exists with id ""(.*)""")]
    public void GivenNoStockExists(string stockId)
    {
        _targetStockId = Guid.Parse(stockId);
        _stockRepo.Setup(r => r.GetByIdAsync(_targetStockId)).ReturnsAsync((Stock?)null);
    }

    [Given(@"the stock has (\d+) existing investments")]
    public void GivenStockHasInvestments(int count)
    {
        var investments = Enumerable.Range(0, count)
            .Select(_ => new Investment { Id = Guid.NewGuid(), StockDetailsId = _targetStockId })
            .ToList();

        _investmentRepo.Setup(r => r.GetInvestmentsByStockId(_targetStockId)).ReturnsAsync(investments);
        _investmentRepo.Setup(r => r.DeleteAsync(It.IsAny<Investment>())).Returns(Task.CompletedTask);
    }

    [When(@"I delete the stock")]
    public async Task WhenIDeleteTheStock()
    {
        _stockRepo.Setup(r => r.DeleteAsync(It.IsAny<Stock>())).Returns(Task.CompletedTask);
        _publisher.Setup(p => p.Publish(It.IsAny<StockRemoved>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new DeleteStockCommandHandler(_stockRepo.Object, _investmentRepo.Object, _publisher.Object);
        await handler.Handle(new DeleteStockCommand(_targetStockId, _ctx.UserId), CancellationToken.None);
    }

    [When(@"I try to delete that stock")]
    public async Task WhenITryToDeleteThatStock()
    {
        _investmentRepo.Setup(r => r.GetInvestmentsByStockId(It.IsAny<Guid>())).ReturnsAsync([]);
        var handler = new DeleteStockCommandHandler(_stockRepo.Object, _investmentRepo.Object, _publisher.Object);
        try { await handler.Handle(new DeleteStockCommand(_targetStockId, _ctx.UserId), CancellationToken.None); }
        catch (Exception ex) { _thrownException = ex; }
    }

    [Then(@"all (\d+) investments should be deleted")]
    public void ThenAllInvestmentsDeleted(int count) =>
        _investmentRepo.Verify(r => r.DeleteAsync(It.IsAny<Investment>()), Times.Exactly(count));

    [Then(@"no investments should be deleted")]
    public void ThenNoInvestmentsDeleted() =>
        _investmentRepo.Verify(r => r.DeleteAsync(It.IsAny<Investment>()), Times.Never);

    [Then(@"the stock itself should be deleted")]
    public void ThenStockDeleted() =>
        _stockRepo.Verify(r => r.DeleteAsync(It.IsAny<Stock>()), Times.Once);

    [Then(@"a StockRemoved event should be published for ticker ""(.*)""")]
    public void ThenStockRemovedPublished(string ticker) =>
        _publisher.Verify(p => p.Publish(
            It.Is<StockRemoved>(e => e.Ticker == ticker && e.UserId == _ctx.UserId),
            It.IsAny<CancellationToken>()), Times.Once);

    [Then(@"a not found error should be raised")]
    public void ThenNotFoundError() =>
        _thrownException.Should().BeOfType<NotFoundException>();

    [Then(@"a bad request error should be raised with message ""(.*)""")]
    public void ThenBadRequestError(string messagePart) =>
        _thrownException.Should().BeOfType<BadRequestException>()
            .Which.Message.Should().Contain(messagePart);
}
