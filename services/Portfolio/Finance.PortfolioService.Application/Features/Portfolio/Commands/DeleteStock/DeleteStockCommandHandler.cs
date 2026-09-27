using Finance.Contracts.Events;
using Finance.PortfolioService.Application.Contracts.Persistence;
using Finance.PortfolioService.Application.Entities;
using Finance.SharedKernel.Auth.Exceptions;
using MassTransit;
using MediatR;

namespace Finance.PortfolioService.Application.Features.Portfolio.Commands.DeleteStock;

public class DeleteStockCommandHandler : IRequestHandler<DeleteStockCommand>
{
    private readonly IStockRepository _stockRepository;
    private readonly IPublishEndpoint _publisher;

    public DeleteStockCommandHandler(
        IStockRepository stockRepository,
        IPublishEndpoint publisher)
    {
        _stockRepository = stockRepository;
        _publisher = publisher;
    }

    public async Task Handle(DeleteStockCommand request, CancellationToken cancellationToken)
    {
        var stock = await _stockRepository.GetByIdAsync(request.StockId);

        if (stock is null)
            throw new NotFoundException(nameof(Stock), request.StockId);

        if (stock.UserId != request.UserId)
            throw new BadRequestException("You do not have permission to delete this stock.");

        // One delete, one transaction. Investments.StockDetailsId is declared ON DELETE CASCADE
        // (see PortfolioDbContextModelSnapshot), so the database removes this stock's investments
        // as part of the same statement.
        //
        // This previously looped over the investments calling DeleteAsync on each, and because
        // every repository mutator commits on its own, deleting a stock with N investments ran
        // N+1 separate transactions. A failure partway through left the portfolio holding
        // investments whose stock was already gone, and StockRemoved could still be published
        // after that partial delete.
        await _stockRepository.DeleteAsync(stock);

        // Published after the commit, so a publish failure leaves the delete in place rather than
        // announcing a removal that did not happen. Closing the remaining gap -- a delete that
        // commits but whose event never publishes -- needs MassTransit's transactional outbox.
        await _publisher.Publish(new StockRemoved(request.UserId, stock.Ticker, DateTime.UtcNow), cancellationToken);
    }
}
