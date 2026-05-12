using Finance.Contracts.Events;
using Finance.PortfolioService.Application.Contracts.Persistence;
using Finance.PortfolioService.Application.Exceptions;
using Finance.PortfolioService.Domain.Entities;
using MassTransit;
using MediatR;

namespace Finance.PortfolioService.Application.Features.Portfolio.Commands.DeleteStock;

public class DeleteStockCommandHandler : IRequestHandler<DeleteStockCommand>
{
    private readonly IStockRepository _stockRepository;
    private readonly IInvestmentRepository _investmentRepository;
    private readonly IPublishEndpoint _publisher;

    public DeleteStockCommandHandler(
        IStockRepository stockRepository,
        IInvestmentRepository investmentRepository,
        IPublishEndpoint publisher)
    {
        _stockRepository = stockRepository;
        _investmentRepository = investmentRepository;
        _publisher = publisher;
    }

    public async Task Handle(DeleteStockCommand request, CancellationToken cancellationToken)
    {
        var stock = await _stockRepository.GetByIdAsync(request.StockId);

        if (stock is null)
            throw new NotFoundException(nameof(Stock), request.StockId);

        if (stock.UserId != request.UserId)
            throw new BadRequestException("You do not have permission to delete this stock.");

        var investments = await _investmentRepository.GetInvestmentsByStockId(request.StockId);
        foreach (var investment in investments)
            await _investmentRepository.DeleteAsync(investment);

        await _stockRepository.DeleteAsync(stock);

        await _publisher.Publish(new StockRemoved(request.UserId, stock.Ticker, DateTime.UtcNow), cancellationToken);
    }
}
