using Finance.PortfolioService.Application.Contracts.Persistence;
using Finance.PortfolioService.Application.Exceptions;
using Finance.PortfolioService.Domain.Entities;
using MediatR;

namespace Finance.PortfolioService.Application.Features.StockSector.Commands.DeleteStockSector;

public class DeleteStockSectorCommandHandler : IRequestHandler<DeleteStockSectorCommand, Unit>
{
    private readonly IStockSectorRepository _stockSectorRepository;

    public DeleteStockSectorCommandHandler(IStockSectorRepository stockSectorRepository)
    {
        _stockSectorRepository = stockSectorRepository;
    }

    public async Task<Unit> Handle(DeleteStockSectorCommand request, CancellationToken cancellationToken)
    {
        var stockSector = await _stockSectorRepository.GetByIdAsync(request.Id)
            ?? throw new NotFoundException(nameof(StockSector), request.Id);

        await _stockSectorRepository.DeleteAsync(stockSector);
        return Unit.Value;
    }
}
