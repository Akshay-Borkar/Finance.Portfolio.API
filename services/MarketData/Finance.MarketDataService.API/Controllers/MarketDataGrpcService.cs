using Finance.MarketDataService.API.Protos;
using Finance.MarketDataService.Application.Contracts;
using Finance.MarketDataService.Infrastructure.Constants;
using Grpc.Core;

namespace Finance.MarketDataService.API.Controllers;

public class MarketDataGrpcService : MarketDataGrpc.MarketDataGrpcBase
{
    private readonly IStockQuoteService _stockQuote;
    private readonly IRedisCacheService _cache;
    private readonly ILogger<MarketDataGrpcService> _logger;

    public MarketDataGrpcService(
        IStockQuoteService stockQuote,
        IRedisCacheService cache,
        ILogger<MarketDataGrpcService> logger)
    {
        _stockQuote = stockQuote;
        _cache = cache;
        _logger = logger;
    }

    public override async Task<GetPriceResponse> GetCurrentPrice(
        GetPriceRequest request, ServerCallContext context)
    {
        var ticker = request.Ticker;

        // Try Redis cache first
        var cached = await _cache.GetCacheAsync<decimal>($"{MarketDataConstants.Redis.PriceCacheKeyPrefix}{ticker}");
        if (cached > 0)
            return new GetPriceResponse
            {
                Ticker    = ticker,
                Price     = (double)cached,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

        // Cache miss — fetch live from Yahoo Finance
        var response = await _stockQuote.FetchStockQuoteAsync(ticker);
        var price = response?.Chart?.Result?.FirstOrDefault()?.Meta?.RegularMarketPrice ?? 0;

        if (price > 0)
            await _cache.SetCacheAsync($"{MarketDataConstants.Redis.PriceCacheKeyPrefix}{ticker}", price, MarketDataConstants.Redis.PriceCacheTtlMinutes);

        _logger.LogInformation("gRPC GetCurrentPrice {Ticker} → {Price}", ticker, price);

        return new GetPriceResponse
        {
            Ticker    = ticker,
            Price     = (double)price,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
    }

    public override async Task<GetOhlcvResponse> GetOhlcv(
        GetOhlcvRequest request, ServerCallContext context)
    {
        var bars = await _stockQuote.FetchOhlcvAsync(
            request.Ticker,
            string.IsNullOrEmpty(request.Interval) ? MarketDataConstants.OhlcvDefaults.Interval : request.Interval,
            string.IsNullOrEmpty(request.Range) ? MarketDataConstants.OhlcvDefaults.Range : request.Range);

        var response = new GetOhlcvResponse();
        response.Bars.AddRange(bars.Select(b => new Protos.OhlcvBar
        {
            Time   = b.Time,
            Open   = (double)b.Open,
            High   = (double)b.High,
            Low    = (double)b.Low,
            Close  = (double)b.Close,
            Volume = b.Volume
        }));

        return response;
    }
}
