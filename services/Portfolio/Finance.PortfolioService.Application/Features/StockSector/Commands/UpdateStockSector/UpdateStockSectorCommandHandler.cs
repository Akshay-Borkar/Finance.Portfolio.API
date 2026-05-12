using Finance.PortfolioService.Application.Contracts.Persistence;
using Finance.PortfolioService.Application.Exceptions;
using Finance.PortfolioService.Domain.Entities;
using MediatR;

namespace Finance.PortfolioService.Application.Features.StockSector.Commands.UpdateStockSector;

public class UpdateStockSectorCommandHandler : IRequestHandler<UpdateStockSectorCommand, Unit>
{
    private readonly IStockSectorRepository _stockSectorRepository;

    public UpdateStockSectorCommandHandler(IStockSectorRepository stockSectorRepository)
    {
        _stockSectorRepository = stockSectorRepository;
    }

    public async Task<Unit> Handle(UpdateStockSectorCommand request, CancellationToken cancellationToken)
    {
        var stockSector = await _stockSectorRepository.GetByIdAsync(request.Id)
            ?? throw new NotFoundException(nameof(StockSector), request.Id);

        stockSector.StockSectorName = request.StockSectorName;
        stockSector.SectorPE = request.SectorPE;

        await _stockSectorRepository.UpdateAsync(stockSector);
        return Unit.Value;
    }
}
