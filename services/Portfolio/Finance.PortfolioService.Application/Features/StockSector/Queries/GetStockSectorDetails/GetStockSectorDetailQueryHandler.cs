using Finance.PortfolioService.Application.Contracts.Persistence;
using Finance.PortfolioService.Application.Exceptions;
using Finance.PortfolioService.Domain.Entities;
using MediatR;

namespace Finance.PortfolioService.Application.Features.StockSector.Queries.GetStockSectorDetails;

public class GetStockSectorDetailQueryHandler : IRequestHandler<GetStockSectorDetailQuery, StockSectorDetailDto>
{
    private readonly IStockSectorRepository _stockSectorRepository;

    public GetStockSectorDetailQueryHandler(IStockSectorRepository stockSectorRepository)
    {
        _stockSectorRepository = stockSectorRepository;
    }

    public async Task<StockSectorDetailDto> Handle(GetStockSectorDetailQuery request, CancellationToken cancellationToken)
    {
        var stockSector = await _stockSectorRepository.GetByIdAsync(request.Id)
            ?? throw new NotFoundException(nameof(StockSector), request.Id);

        return new StockSectorDetailDto
        {
            Id = stockSector.Id,
            StockSectorName = stockSector.StockSectorName,
            SectorPE = stockSector.SectorPE,
            DateCreated = stockSector.DateCreated,
            DateModified = stockSector.DateModified
        };
    }
}
