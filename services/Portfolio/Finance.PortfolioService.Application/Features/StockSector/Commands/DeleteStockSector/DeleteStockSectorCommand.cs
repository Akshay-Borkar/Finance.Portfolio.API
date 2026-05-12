using MediatR;

namespace Finance.PortfolioService.Application.Features.StockSector.Commands.DeleteStockSector;

public class DeleteStockSectorCommand : IRequest<Unit>
{
    public Guid Id { get; set; }
}
