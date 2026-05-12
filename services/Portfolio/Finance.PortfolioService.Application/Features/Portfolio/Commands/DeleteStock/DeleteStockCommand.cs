using MediatR;

namespace Finance.PortfolioService.Application.Features.Portfolio.Commands.DeleteStock;

public record DeleteStockCommand(Guid StockId, Guid UserId) : IRequest;
