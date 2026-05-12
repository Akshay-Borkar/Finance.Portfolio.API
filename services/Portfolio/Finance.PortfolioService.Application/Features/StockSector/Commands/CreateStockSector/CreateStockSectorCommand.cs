using MediatR;

namespace Finance.PortfolioService.Application.Features.StockSector.Commands.CreateStockSector;

public class CreateStockSectorCommand : IRequest<Guid>
{
    public string StockSectorName { get; set; } = string.Empty;
    public double? SectorPE { get; set; }
}
