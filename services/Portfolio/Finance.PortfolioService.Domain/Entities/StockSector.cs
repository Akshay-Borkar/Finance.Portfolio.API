using Finance.PortfolioService.Domain.Common;

namespace Finance.PortfolioService.Domain.Entities;

public class StockSector : BaseEntity
{
    public string StockSectorName { get; set; } = string.Empty;
    public double? SectorPE { get; set; }
}
