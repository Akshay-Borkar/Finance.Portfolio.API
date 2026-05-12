using MediatR;

namespace Finance.PortfolioService.Application.Features.Portfolio.Commands.AddStock;

public class AddStockCommand : IRequest<Guid>
{
    public string Ticker { get; set; } = string.Empty;
    public string StockName { get; set; } = string.Empty;
    public double? StockPE { get; set; }
    public Guid StockSectorId { get; set; }
    public Guid UserId { get; set; }
}
