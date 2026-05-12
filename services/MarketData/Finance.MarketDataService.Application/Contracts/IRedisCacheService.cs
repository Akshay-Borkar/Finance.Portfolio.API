namespace Finance.MarketDataService.Application.Contracts;

public interface IRedisCacheService
{
    Task SetCacheAsync<T>(string key, T data, int expirationMinutes);
    Task<T?> GetCacheAsync<T>(string key);
    Task RemoveCacheAsync(string key);

    // Active ticker registry — replaces the hardcoded "testUser" key
    Task AddActiveTickerAsync(string ticker);
    Task RemoveActiveTickerAsync(string ticker);
    Task<List<string>> GetActiveTickersAsync();
}
