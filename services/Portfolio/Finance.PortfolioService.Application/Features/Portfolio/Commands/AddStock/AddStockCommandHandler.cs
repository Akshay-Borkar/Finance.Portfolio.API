using Finance.Contracts.Events;
using Finance.PortfolioService.Application.Contracts.MarketData;
using Finance.PortfolioService.Application.Contracts.Persistence;
using Finance.PortfolioService.Domain.Entities;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Finance.PortfolioService.Application.Features.Portfolio.Commands.AddStock;

public class AddStockCommandHandler : IRequestHandler<AddStockCommand, Guid>
{
    private readonly IStockRepository _stockRepository;
    private readonly IMarketDataGrpcClient _marketData;
    private readonly IPublishEndpoint _publisher;
    private readonly ILogger<AddStockCommandHandler> _logger;

    public AddStockCommandHandler(
        IStockRepository stockRepository,
        IMarketDataGrpcClient marketData,
        IPublishEndpoint publisher,
        ILogger<AddStockCommandHandler> logger)
    {
        _stockRepository = stockRepository;
        _marketData = marketData;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<Guid> Handle(AddStockCommand request, CancellationToken cancellationToken)
    {
        decimal currentPrice = 0m;
        try
        {
            currentPrice = await _marketData.GetCurrentPriceAsync(request.Ticker, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch current price for {Ticker} from Market Data service, defaulting to 0", request.Ticker);
        }

        var stock = new Stock
        {
            Id = Guid.NewGuid(),
            Ticker = request.Ticker.ToUpperInvariant(),
            StockName = request.StockName,
            CurrentPrice = currentPrice,
            StockPE = request.StockPE,
            UserId = request.UserId,
            StockSectorId = request.StockSectorId
        };

        await _stockRepository.CreateAsync(stock);

        await _publisher.Publish(new StockAdded(request.UserId, stock.Ticker, DateTime.UtcNow), cancellationToken);

        return stock.Id;
    }
}
