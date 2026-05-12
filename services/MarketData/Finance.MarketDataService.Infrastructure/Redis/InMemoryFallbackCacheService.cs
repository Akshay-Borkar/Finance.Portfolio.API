using Finance.MarketDataService.Application.Contracts;
using Microsoft.Extensions.Caching.Memory;

namespace Finance.MarketDataService.Infrastructure.Redis;

// Used when Redis is unavailable (local dev without Docker).
public class InMemoryFallbackCacheService : IRedisCacheService
{
    private static readonly IMemoryCache Cache = new MemoryCache(new MemoryCacheOptions());
    private static readonly HashSet<string> ActiveTickers = [];

    public Task SetCacheAsync<T>(string key, T data, int expirationMinutes)
    {
        Cache.Set(key, data, TimeSpan.FromMinutes(expirationMinutes));
        return Task.CompletedTask;
    }

    public Task<T?> GetCacheAsync<T>(string key)
    {
        Cache.TryGetValue(key, out T? v);
        return Task.FromResult(v);
    }

    public Task RemoveCacheAsync(string key) { Cache.Remove(key); return Task.CompletedTask; }

    public Task AddActiveTickerAsync(string ticker)    { lock (ActiveTickers) ActiveTickers.Add(ticker);    return Task.CompletedTask; }
    public Task RemoveActiveTickerAsync(string ticker) { lock (ActiveTickers) ActiveTickers.Remove(ticker); return Task.CompletedTask; }
    public Task<List<string>> GetActiveTickersAsync()  { lock (ActiveTickers) return Task.FromResult(ActiveTickers.ToList()); }
}
