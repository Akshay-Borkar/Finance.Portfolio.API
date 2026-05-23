using Finance.Contracts.Events;
using Finance.PortfolioService.Application.Contracts.MarketData;
using Finance.PortfolioService.Application.Contracts.Persistence;
using Finance.PortfolioService.Application.Features.Portfolio.Commands.AddStock;
using Finance.PortfolioService.Domain.Entities;
using Finance.PortfolioService.Tests.Support;
using FluentAssertions;
using MassTransit;
using Moq;
using Reqnroll;

namespace Finance.PortfolioService.Tests.StepDefinitions;

[Binding]
public class AddStockSteps
{
    private readonly PortfolioTestContext _ctx;
    private readonly Mock<IStockRepository> _stockRepo = new();
    private readonly Mock<IMarketDataGrpcClient> _marketData = new();
    private readonly Mock<IPublishEndpoint> _publisher = new();

    private Guid _result;
    private Stock? _capturedStock;

    public AddStockSteps(PortfolioTestContext ctx) => _ctx = ctx;

    [Given(@"the market data service returns a price of (.*) for ticker ""(.*)""")]
    public void GivenMarketDataReturnsPrice(decimal price, string ticker) =>
        _marketData
            .Setup(m => m.GetCurrentPriceAsync(ticker, It.IsAny<CancellationToken>()))
            .ReturnsAsync(price);

    [Given(@"the market data service is unavailable for ticker ""(.*)""")]
    public void GivenMarketDataIsUnavailable(string ticker) =>
        _marketData
            .Setup(m => m.GetCurrentPriceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("gRPC service unavailable"));

    [When(@"I add a stock with ticker ""(.*)"" and name ""(.*)"" to the portfolio")]
    public async Task WhenIAddAStock(string ticker, string name)
    {
        _stockRepo
            .Setup(r => r.CreateAsync(It.IsAny<Stock>()))
            .Callback<Stock>(s => _capturedStock = s)
            .Returns(Task.CompletedTask);
        _publisher
            .Setup(p => p.Publish(It.IsAny<StockAdded>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new AddStockCommandHandler(_stockRepo.Object, _marketData.Object, _publisher.Object);
        _result = await handler.Handle(new AddStockCommand
        {
            Ticker = ticker,
            StockName = name,
            UserId = _ctx.UserId,
            StockSectorId = _ctx.SectorId
        }, CancellationToken.None);
    }

    [Then(@"a new stock should be created with ticker ""(.*)""")]
    public void ThenStockCreatedWithTicker(string ticker) =>
        _capturedStock!.Ticker.Should().Be(ticker);

    [Then(@"the stock should have a current price of (.*)")]
    public void ThenStockHasPrice(decimal price) =>
        _capturedStock!.CurrentPrice.Should().Be(price);

    [Then(@"a StockAdded event should be published for ticker ""(.*)""")]
    public void ThenStockAddedPublished(string ticker) =>
        _publisher.Verify(p => p.Publish(
            It.Is<StockAdded>(e => e.Ticker == ticker && e.UserId == _ctx.UserId),
            It.IsAny<CancellationToken>()), Times.Once);

    [Then(@"a StockAdded event should still be published for ticker ""(.*)""")]
    public void ThenStockAddedStillPublished(string ticker) => ThenStockAddedPublished(ticker);

    [Then(@"the returned stock id should not be empty")]
    public void ThenReturnedIdNotEmpty() =>
        _result.Should().NotBe(Guid.Empty);

    [Then(@"the stock should belong to user ""(.*)""")]
    public void ThenStockBelongsToUser(string userId) =>
        _capturedStock!.UserId.Should().Be(Guid.Parse(userId));

    [Then(@"the stock should belong to sector ""(.*)""")]
    public void ThenStockBelongsToSector(string sectorId) =>
        _capturedStock!.StockSectorId.Should().Be(Guid.Parse(sectorId));
}
