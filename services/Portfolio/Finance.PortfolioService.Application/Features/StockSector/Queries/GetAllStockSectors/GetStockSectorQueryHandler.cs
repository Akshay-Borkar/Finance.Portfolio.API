using Finance.PortfolioService.Application.Contracts.Persistence;
using MediatR;

namespace Finance.PortfolioService.Application.Features.StockSector.Queries.GetAllStockSectors;

public class GetStockSectorQueryHandler : IRequestHandler<GetStockSectorQuery, List<StockSectorDto>>
{
    private readonly IStockSectorRepository _stockSectorRepository;

    public GetStockSectorQueryHandler(IStockSectorRepository stockSectorRepository)
    {
        _stockSectorRepository = stockSectorRepository;
    }

    public async Task<List<StockSectorDto>> Handle(GetStockSectorQuery request, CancellationToken cancellationToken)
    {
        var stockSectors = await _stockSectorRepository.GetAsync();
        return stockSectors.Select(s => new StockSectorDto
        {
            Id = s.Id,
            StockSectorName = s.StockSectorName,
            SectorPE = s.SectorPE
        }).ToList();
    }
}
