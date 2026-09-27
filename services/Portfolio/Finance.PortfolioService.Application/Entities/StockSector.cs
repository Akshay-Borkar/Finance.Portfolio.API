using Finance.PortfolioService.Application.Common;

namespace Finance.PortfolioService.Application.Entities;

public class StockSector : BaseEntity
{
    public string StockSectorName { get; set; } = string.Empty;
    public double? SectorPE { get; set; }
}
