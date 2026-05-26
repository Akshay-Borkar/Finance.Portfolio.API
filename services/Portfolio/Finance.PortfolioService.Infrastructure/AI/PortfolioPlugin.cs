using System.ComponentModel;
using System.Text;
using System.Text.Json;
using Finance.PortfolioService.Application.Features.Portfolio.Queries.GetPortfolioSummary;
using MediatR;
using Microsoft.SemanticKernel;

namespace Finance.PortfolioService.Infrastructure.AI;

public class PortfolioPlugin
{
    private readonly IMediator _mediator;
    private readonly Guid _userId;

    public PortfolioPlugin(IMediator mediator, Guid userId)
    {
        _mediator = mediator;
        _userId = userId;
    }

    [KernelFunction("get_portfolio_summary")]
    [Description("Retrieves the user's complete portfolio summary including total invested amount, current value, overall profit/loss, all stock holdings with individual P&L, and sector-wise allocation. Call this first before answering any question about the user's portfolio performance, investments, or overall financial position.")]
    public async Task<string> GetPortfolioSummaryAsync()
    {
        var summary = await _mediator.Send(new GetPortfolioSummaryQuery(_userId));

        var sb = new StringBuilder();
        sb.AppendLine("=== PORTFOLIO SUMMARY ===");
        sb.AppendLine($"Total Invested: ₹{summary.TotalInvested:N2}");
        sb.AppendLine($"Current Value:  ₹{summary.CurrentValue:N2}");
        sb.AppendLine($"Total P&L:      {(summary.TotalPnL >= 0 ? "+" : "")}₹{summary.TotalPnL:N2} ({(summary.TotalPnLPercent >= 0 ? "+" : "")}{summary.TotalPnLPercent:N2}%)");

        sb.AppendLine();
        sb.AppendLine("=== HOLDINGS ===");
        foreach (var h in summary.Holdings)
        {
            sb.AppendLine($"[{h.Ticker}] {h.StockName} | Sector: {h.SectorName}");
            sb.AppendLine($"  Qty: {h.Quantity} | Avg Buy: ₹{h.AvgBuyingPrice:N2} | Current: ₹{h.CurrentPrice:N2}");
            sb.AppendLine($"  Invested: ₹{h.InvestedAmount:N2} | Current Value: ₹{h.CurrentValue:N2}");
            sb.AppendLine($"  P&L: {(h.PnL >= 0 ? "+" : "")}₹{h.PnL:N2} ({(h.PnLPercent >= 0 ? "+" : "")}{h.PnLPercent:N2}%)");
        }

        sb.AppendLine();
        sb.AppendLine("=== SECTOR ALLOCATION ===");
        foreach (var s in summary.SectorAllocations)
        {
            sb.AppendLine($"{s.SectorName}: ₹{s.InvestedAmount:N2} ({s.AllocationPercent:N2}%)");
        }

        return sb.ToString();
    }

    [KernelFunction("get_sector_allocation")]
    [Description("Returns the current sector-wise allocation of the portfolio including sector name, percentage of total portfolio, and amount invested per sector.")]
    public async Task<string> GetSectorAllocationAsync()
    {
        var summary = await _mediator.Send(new GetPortfolioSummaryQuery(_userId));

        var sb = new StringBuilder();
        sb.AppendLine("=== SECTOR ALLOCATION ===");
        foreach (var s in summary.SectorAllocations)
        {
            sb.AppendLine($"{s.SectorName}: {s.AllocationPercent:N2}% | ₹{s.InvestedAmount:N2}");
        }

        return sb.ToString();
    }

    [KernelFunction("calculate_rebalance_plan")]
    [Description("Calculates a dry-run rebalancing plan to reach a target sector allocation. The targetAllocationJson parameter should be a JSON object like {\"IT\":30,\"Banking\":25} where values are target percentages that must sum to 100.")]
    public async Task<string> CalculateRebalancePlanAsync(
        [Description("Target sector allocation as JSON, e.g. {\"IT\":30,\"Banking\":25}. Values are percentages and must sum to 100.")] string targetAllocationJson)
    {
        Dictionary<string, double> targets;
        try
        {
            targets = JsonSerializer.Deserialize<Dictionary<string, double>>(targetAllocationJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? throw new InvalidOperationException("Empty JSON");
        }
        catch
        {
            return "ERROR: Could not parse targetAllocationJson. Expected format: {\"IT\":30,\"Banking\":25}";
        }

        var total = targets.Values.Sum();
        if (Math.Abs(total - 100.0) > 0.01)
            return $"ERROR: Target allocations must sum to 100%. Provided sum: {total:N2}%";

        var summary = await _mediator.Send(new GetPortfolioSummaryQuery(_userId));
        var portfolioValue = summary.TotalInvested;

        var currentBySector = summary.SectorAllocations
            .ToDictionary(s => s.SectorName, s => s.InvestedAmount, StringComparer.OrdinalIgnoreCase);

        var sb = new StringBuilder();
        sb.AppendLine("=== REBALANCING PLAN (DRY RUN) ===");
        sb.AppendLine($"Total Portfolio Value: ₹{portfolioValue:N2}");
        sb.AppendLine();

        var largeTradeThreshold = portfolioValue * 0.05;

        foreach (var (sector, targetPct) in targets)
        {
            var targetAmount = portfolioValue * (targetPct / 100.0);
            currentBySector.TryGetValue(sector, out var currentAmount);
            var diff = targetAmount - currentAmount;
            var action = diff >= 0 ? "BUY" : "SELL";
            var absDiff = Math.Abs(diff);
            var warning = absDiff > largeTradeThreshold ? " ⚠️ LARGE TRADE" : string.Empty;

            sb.AppendLine($"{sector}: {action} ₹{absDiff:N2} (Current: ₹{currentAmount:N2} → Target: ₹{targetAmount:N2} [{targetPct}%]){warning}");
        }

        // Flag sectors in portfolio that have no target (will be reduced to 0)
        foreach (var (sector, invested) in currentBySector)
        {
            if (!targets.ContainsKey(sector) && invested > 0)
            {
                var warning = invested > largeTradeThreshold ? " ⚠️ LARGE TRADE" : string.Empty;
                sb.AppendLine($"{sector}: SELL ₹{invested:N2} (not in target allocation){warning}");
            }
        }

        sb.AppendLine();
        sb.AppendLine("NOTE: This is a DRY RUN. No trades have been executed.");
        return sb.ToString();
    }

    [KernelFunction("check_market_hours")]
    [Description("Checks whether the Indian stock market (NSE/BSE) is currently open. Market hours are Monday–Friday 9:15 AM to 3:30 PM IST.")]
    public Task<string> CheckMarketHoursAsync()
    {
        var ist = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ist);
        var open = new TimeOnly(9, 15);
        var close = new TimeOnly(15, 30);
        var timeNow = TimeOnly.FromDateTime(now);

        bool isWeekday = now.DayOfWeek is >= DayOfWeek.Monday and <= DayOfWeek.Friday;
        bool isWithinHours = timeNow >= open && timeNow <= close;
        bool isOpen = isWeekday && isWithinHours;

        var status = isOpen ? "OPEN" : "CLOSED";
        return Task.FromResult(
            $"Market Status: {status}\nCurrent IST Time: {now:dddd, dd MMM yyyy HH:mm:ss}\nNSE/BSE Hours: Mon–Fri 9:15 AM – 3:30 PM IST");
    }

    [KernelFunction("get_holding_detail")]
    [Description("Retrieves detailed information about a specific stock holding identified by its ticker symbol (e.g. 'RELIANCE', 'TCS', 'INFY'). Use this when the user asks about a particular stock in their portfolio. If the ticker is not found, lists all available tickers.")]
    public async Task<string> GetHoldingDetailAsync([Description("The stock ticker symbol to look up, e.g. 'RELIANCE' or 'TCS'")] string ticker)
    {
        var summary = await _mediator.Send(new GetPortfolioSummaryQuery(_userId));

        var holding = summary.Holdings.FirstOrDefault(h =>
            string.Equals(h.Ticker, ticker, StringComparison.OrdinalIgnoreCase));

        if (holding is null)
        {
            var tickers = string.Join(", ", summary.Holdings.Select(h => h.Ticker));
            return $"No holding found for ticker '{ticker}'. Available tickers: {tickers}";
        }

        return $"""
            === {holding.StockName} ({holding.Ticker}) ===
            Sector: {holding.SectorName}
            Quantity: {holding.Quantity}
            Avg Buying Price: ₹{holding.AvgBuyingPrice:N2}
            Current Price:    ₹{holding.CurrentPrice:N2}
            Invested Amount:  ₹{holding.InvestedAmount:N2}
            Current Value:    ₹{holding.CurrentValue:N2}
            P&L:              {(holding.PnL >= 0 ? "+" : "")}₹{holding.PnL:N2} ({(holding.PnLPercent >= 0 ? "+" : "")}{holding.PnLPercent:N2}%)
            """;
    }
}
