namespace Finance.PortfolioService.Application.Features.Portfolio.Queries.GetPortfolioSummary;

public class PortfolioSummaryDto
{
    public double TotalInvested { get; set; }
    public double CurrentValue { get; set; }
    public double TotalPnL { get; set; }
    public double TotalPnLPercent { get; set; }
    public List<PortfolioHoldingDto> Holdings { get; set; } = [];
    public List<SectorAllocationDto> SectorAllocations { get; set; } = [];
}

public class PortfolioHoldingDto
{
    public Guid StockId { get; set; }
    public string Ticker { get; set; } = string.Empty;
    public string StockName { get; set; } = string.Empty;
    public string SectorName { get; set; } = string.Empty;
    public double Quantity { get; set; }
    public double AvgBuyingPrice { get; set; }
    public double CurrentPrice { get; set; }
    public double InvestedAmount { get; set; }
    public double CurrentValue { get; set; }
    public double PnL { get; set; }
    public double PnLPercent { get; set; }
}

public class SectorAllocationDto
{
    public string SectorName { get; set; } = string.Empty;
    public double InvestedAmount { get; set; }
    public double AllocationPercent { get; set; }
}
