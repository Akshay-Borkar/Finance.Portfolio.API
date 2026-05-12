using MediatR;

namespace Finance.PortfolioService.Application.Features.StockSector.Queries.GetAllStockSectors;

public record GetStockSectorQuery : IRequest<List<StockSectorDto>>;
