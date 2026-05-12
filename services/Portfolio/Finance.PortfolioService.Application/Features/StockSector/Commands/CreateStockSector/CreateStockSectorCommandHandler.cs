using Finance.PortfolioService.Application.Contracts.Persistence;
using Finance.PortfolioService.Application.Exceptions;
using Finance.PortfolioService.Domain.Entities;
using MediatR;

namespace Finance.PortfolioService.Application.Features.StockSector.Commands.CreateStockSector;

public class CreateStockSectorCommandHandler : IRequestHandler<CreateStockSectorCommand, Guid>
{
    private readonly IStockSectorRepository _stockSectorRepository;

    public CreateStockSectorCommandHandler(IStockSectorRepository stockSectorRepository)
    {
        _stockSectorRepository = stockSectorRepository;
    }

    public async Task<Guid> Handle(CreateStockSectorCommand request, CancellationToken cancellationToken)
    {
        var validator = new CreateStockSectorCommandValidator(_stockSectorRepository);
        var validationResult = await validator.ValidateAsync(request, cancellationToken);

        if (validationResult.Errors.Count > 0)
            throw new BadRequestException("Invalid Stock Sector", validationResult);

        var stockSector = new Domain.Entities.StockSector
        {
            Id = Guid.NewGuid(),
            StockSectorName = request.StockSectorName,
            SectorPE = request.SectorPE
        };

        await _stockSectorRepository.CreateAsync(stockSector);
        return stockSector.Id;
    }
}
