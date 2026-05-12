using MediatR;

namespace Finance.PortfolioService.Application.Features.StockSector.Queries.GetStockSectorDetails;

public record GetStockSectorDetailQuery(Guid Id) : IRequest<StockSectorDetailDto>;
