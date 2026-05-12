namespace Finance.PortfolioService.Application.Features.StockSector.Queries.GetStockSectorDetails;

public class StockSectorDetailDto
{
    public Guid Id { get; set; }
    public string StockSectorName { get; set; } = string.Empty;
    public double? SectorPE { get; set; }
    public DateTime DateCreated { get; set; }
    public DateTime DateModified { get; set; }
}
