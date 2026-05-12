using Finance.PortfolioService.Domain.Common;

namespace Finance.PortfolioService.Domain.Entities;

public class Stock : BaseEntity
{
    public string Ticker { get; set; } = string.Empty;
    public string StockName { get; set; } = string.Empty;
    public decimal CurrentPrice { get; set; }
    public decimal? MarketCap { get; set; }
    public double? StockPE { get; set; }
    public Guid UserId { get; set; }
    public Guid StockSectorId { get; set; }
    public StockSector StockSector { get; set; } = null!;
}
