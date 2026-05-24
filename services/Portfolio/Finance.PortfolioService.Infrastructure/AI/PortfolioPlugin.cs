using System.ComponentModel;
using System.Text;
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
