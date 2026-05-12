using MediatR;

namespace Finance.PortfolioService.Application.Features.StockSector.Commands.UpdateStockSector;

public class UpdateStockSectorCommand : IRequest<Unit>
{
    public Guid Id { get; set; }
    public string StockSectorName { get; set; } = string.Empty;
    public double? SectorPE { get; set; }
}
