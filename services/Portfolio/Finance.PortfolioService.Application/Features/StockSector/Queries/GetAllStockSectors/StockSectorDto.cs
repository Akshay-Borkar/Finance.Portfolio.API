namespace Finance.PortfolioService.Application.Features.StockSector.Queries.GetAllStockSectors;

public class StockSectorDto
{
    public Guid Id { get; set; }
    public string StockSectorName { get; set; } = string.Empty;
    public double? SectorPE { get; set; }
}
