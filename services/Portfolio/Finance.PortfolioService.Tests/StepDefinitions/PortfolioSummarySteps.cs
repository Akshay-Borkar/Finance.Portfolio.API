using Finance.PortfolioService.Application.Contracts.MarketData;
using Finance.PortfolioService.Application.Contracts.Persistence;
using Finance.PortfolioService.Application.Features.Portfolio.Queries.GetPortfolioSummary;
using Finance.PortfolioService.Domain.Entities;
using Finance.PortfolioService.Tests.Support;
using FluentAssertions;
using Moq;
using Reqnroll;

namespace Finance.PortfolioService.Tests.StepDefinitions;

[Binding]
public class PortfolioSummarySteps
{
    private readonly PortfolioTestContext _ctx;
    private readonly Mock<IInvestmentRepository> _investmentRepo = new();
    private readonly Mock<IStockRepository> _stockRepo = new();
    private readonly Mock<IMarketDataGrpcClient> _marketData = new();

    private PortfolioSummaryDto _result = new();
    private readonly List<Stock> _stocks = [];
    private readonly List<Investment> _investments = [];

    public PortfolioSummarySteps(PortfolioTestContext ctx) => _ctx = ctx;

    [Given(@"the user has no stocks in their portfolio")]
    public void GivenNoStocks() { _stocks.Clear(); _investments.Clear(); }

    [Given(@"the user owns stock ""(.*)"" in sector ""(.*)"" with (.*) invested at a buying price of (.*)")]
    public void GivenUserOwnsStock(string ticker, string sectorName, decimal invested, double buyingPrice)
    {
        var sector = new StockSector { StockSectorName = sectorName };
        var stock = new Stock
        {
            Id = Guid.NewGuid(), Ticker = ticker, StockName = ticker,
            UserId = _ctx.UserId, CurrentPrice = (decimal)buyingPrice, StockSector = sector
        };
        _stocks.Add(stock);
        _investments.Add(new Investment
        {
            Id = Guid.NewGuid(), InvestedAmount = invested, BuyingPrice = buyingPrice,
            InvestmentDate = DateTime.UtcNow, StockDetailsId = stock.Id, StockDetails = stock
        });
    }

    [Given(@"the market data service returns a current price of (.*) for ""(.*)""")]
    public void GivenMarketDataReturnsPrice(decimal price, string ticker) =>
        _marketData
            .Setup(m => m.GetCurrentPriceAsync(ticker, It.IsAny<CancellationToken>()))
            .ReturnsAsync(price);

    [Given(@"the stored price for ""(.*)"" is (.*)")]
    public void GivenStoredPrice(string ticker, decimal price)
    {
        var stock = _stocks.FirstOrDefault(s => s.Ticker == ticker);
        if (stock is not null) stock.CurrentPrice = price;
    }

    [Given(@"the market data service is unavailable for ""(.*)""")]
    public void GivenMarketDataUnavailable(string ticker) =>
        _marketData
            .Setup(m => m.GetCurrentPriceAsync(ticker, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Service unavailable"));

    [When(@"I request the portfolio summary")]
    public async Task WhenIRequestPortfolioSummary()
    {
        _stockRepo.Setup(r => r.GetStocksByUserId(_ctx.UserId)).ReturnsAsync(_stocks);
        _investmentRepo.Setup(r => r.GetPortfolioByUserId(_ctx.UserId)).ReturnsAsync(_investments);

        var handler = new GetPortfolioSummaryQueryHandler(_investmentRepo.Object, _stockRepo.Object, _marketData.Object);
        _result = await handler.Handle(new GetPortfolioSummaryQuery(_ctx.UserId), CancellationToken.None);
    }

    [Then(@"the summary should show zero holdings")]
    public void ThenZeroHoldings() => _result.Holdings.Should().BeEmpty();

    [Then(@"the total invested should be (.*)")]
    public void ThenTotalInvested(double amount) =>
        _result.TotalInvested.Should().BeApproximately(amount, 0.01);

    [Then(@"the total PnL should be (.*)")]
    public void ThenTotalPnL(double pnl) =>
        _result.TotalPnL.Should().BeApproximately(pnl, 0.01);

    [Then(@"the summary should contain (\d+) holding")]
    public void ThenHoldingCount(int count) =>
        _result.Holdings.Should().HaveCount(count);

    [Then(@"the holding for ""(.*)"" should show a quantity of (.*)")]
    public void ThenHoldingQuantity(string ticker, double qty) =>
        _result.Holdings.First(h => h.Ticker == ticker).Quantity.Should().BeApproximately(qty, 0.001);

    [Then(@"the holding for ""(.*)"" should show a current value of (.*)")]
    public void ThenHoldingCurrentValue(string ticker, double value) =>
        _result.Holdings.First(h => h.Ticker == ticker).CurrentValue.Should().BeApproximately(value, 0.01);

    [Then(@"the holding for ""(.*)"" should show a PnL of (.*)")]
    public void ThenHoldingPnL(string ticker, double pnl) =>
        _result.Holdings.First(h => h.Ticker == ticker).PnL.Should().BeApproximately(pnl, 0.01);

    [Then(@"the sector allocation for ""(.*)"" should be approximately (.*) percent")]
    public void ThenSectorAllocation(string sector, double percent) =>
        _result.SectorAllocations.First(s => s.SectorName == sector)
            .AllocationPercent.Should().BeApproximately(percent, 0.5);

    [Then(@"the holding for ""(.*)"" should use the stored price of (.*)")]
    public void ThenHoldingUsesStoredPrice(string ticker, double price) =>
        _result.Holdings.First(h => h.Ticker == ticker).CurrentPrice.Should().BeApproximately(price, 0.01);
}
