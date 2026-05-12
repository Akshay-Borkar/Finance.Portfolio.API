using Finance.PortfolioService.Application.Contracts.MarketData;
using Finance.PortfolioService.Application.Contracts.Persistence;
using MediatR;

namespace Finance.PortfolioService.Application.Features.Portfolio.Queries.GetPortfolioSummary;

public class GetPortfolioSummaryQueryHandler : IRequestHandler<GetPortfolioSummaryQuery, PortfolioSummaryDto>
{
    private readonly IInvestmentRepository _investmentRepository;
    private readonly IStockRepository _stockRepository;
    private readonly IMarketDataGrpcClient _marketData;

    public GetPortfolioSummaryQueryHandler(
        IInvestmentRepository investmentRepository,
        IStockRepository stockRepository,
        IMarketDataGrpcClient marketData)
    {
        _investmentRepository = investmentRepository;
        _stockRepository = stockRepository;
        _marketData = marketData;
    }

    public async Task<PortfolioSummaryDto> Handle(GetPortfolioSummaryQuery request, CancellationToken cancellationToken)
    {
        var investments = await _investmentRepository.GetPortfolioByUserId(request.UserId);
        var allUserStocks = await _stockRepository.GetStocksByUserId(request.UserId);

        if (allUserStocks.Count == 0)
            return new PortfolioSummaryDto();

        var uniqueTickers = allUserStocks.Select(s => s.Ticker).Distinct().ToList();

        var priceTasks = uniqueTickers.Select(async ticker =>
        {
            try
            {
                var price = await _marketData.GetCurrentPriceAsync(ticker, cancellationToken);
                return (ticker, price: (double)price);
            }
            catch
            {
                var fallback = allUserStocks.FirstOrDefault(s => s.Ticker == ticker)?.CurrentPrice ?? 0m;
                return (ticker, price: (double)fallback);
            }
        });

        var priceResults = await Task.WhenAll(priceTasks);
        var priceMap = priceResults.ToDictionary(r => r.ticker, r => r.price);

        var investmentsByStock = investments
            .GroupBy(i => i.StockDetails.Id)
            .ToDictionary(g => g.Key, g => g.ToList());

        var holdings = new List<PortfolioHoldingDto>();

        foreach (var stock in allUserStocks)
        {
            var currentPrice = priceMap.GetValueOrDefault(stock.Ticker, 0);

            double totalInvested = 0;
            double totalQuantity = 0;

            if (investmentsByStock.TryGetValue(stock.Id, out var stockInvestments))
            {
                foreach (var inv in stockInvestments)
                {
                    double invested = (double)inv.InvestedAmount;
                    double qty = inv.BuyingPrice > 0 ? invested / inv.BuyingPrice : 0;
                    totalInvested += invested;
                    totalQuantity += qty;
                }
            }

            double avgBuyPrice = totalQuantity > 0 ? totalInvested / totalQuantity : 0;
            double currentValue = totalQuantity * currentPrice;
            double pnl = currentValue - totalInvested;
            double pnlPercent = totalInvested > 0 ? (pnl / totalInvested) * 100 : 0;

            holdings.Add(new PortfolioHoldingDto
            {
                StockId = stock.Id,
                Ticker = stock.Ticker,
                StockName = stock.StockName,
                SectorName = stock.StockSector?.StockSectorName ?? "Unknown",
                Quantity = Math.Round(totalQuantity, 4),
                AvgBuyingPrice = Math.Round(avgBuyPrice, 2),
                CurrentPrice = Math.Round(currentPrice, 2),
                InvestedAmount = Math.Round(totalInvested, 2),
                CurrentValue = Math.Round(currentValue, 2),
                PnL = Math.Round(pnl, 2),
                PnLPercent = Math.Round(pnlPercent, 2)
            });
        }

        double portfolioInvested = holdings.Sum(h => h.InvestedAmount);
        double portfolioValue = holdings.Sum(h => h.CurrentValue);
        double totalPnL = portfolioValue - portfolioInvested;
        double totalPnLPercent = portfolioInvested > 0 ? (totalPnL / portfolioInvested) * 100 : 0;

        var sectorAllocations = holdings
            .GroupBy(h => h.SectorName)
            .Select(g => new SectorAllocationDto
            {
                SectorName = g.Key,
                InvestedAmount = Math.Round(g.Sum(h => h.InvestedAmount), 2),
                AllocationPercent = portfolioInvested > 0
                    ? Math.Round(g.Sum(h => h.InvestedAmount) / portfolioInvested * 100, 2)
                    : 0
            })
            .OrderByDescending(s => s.AllocationPercent)
            .ToList();

        return new PortfolioSummaryDto
        {
            TotalInvested = Math.Round(portfolioInvested, 2),
            CurrentValue = Math.Round(portfolioValue, 2),
            TotalPnL = Math.Round(totalPnL, 2),
            TotalPnLPercent = Math.Round(totalPnLPercent, 2),
            Holdings = holdings.OrderByDescending(h => h.InvestedAmount).ToList(),
            SectorAllocations = sectorAllocations
        };
    }
}
