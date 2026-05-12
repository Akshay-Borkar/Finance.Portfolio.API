using Finance.MarketDataService.API.Protos;
using Finance.PortfolioService.Application.Contracts.MarketData;

namespace Finance.PortfolioService.Infrastructure.GrpcClients;

public class MarketDataGrpcClient : IMarketDataGrpcClient
{
    private readonly MarketDataGrpc.MarketDataGrpcClient _client;

    public MarketDataGrpcClient(MarketDataGrpc.MarketDataGrpcClient client)
    {
        _client = client;
    }

    public async Task<decimal> GetCurrentPriceAsync(string ticker, CancellationToken cancellationToken = default)
    {
        var response = await _client.GetCurrentPriceAsync(
            new GetPriceRequest { Ticker = ticker },
            cancellationToken: cancellationToken);
        return (decimal)response.Price;
    }

    public async Task<List<OhlcvBarDto>> GetOhlcvAsync(string ticker, string interval, string range, CancellationToken cancellationToken = default)
    {
        var response = await _client.GetOhlcvAsync(
            new GetOhlcvRequest { Ticker = ticker, Interval = interval, Range = range },
            cancellationToken: cancellationToken);

        return response.Bars.Select(b => new OhlcvBarDto(b.Time, b.Open, b.High, b.Low, b.Close, b.Volume)).ToList();
    }
}
