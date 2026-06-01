using Finance.MarketDataService.Application.Contracts;
using Finance.MarketDataService.Infrastructure.Constants;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Finance.MarketDataService.Infrastructure.Redis;

public class RedisCacheService : IRedisCacheService
{
    private readonly IDatabase _db;
    private static string ActiveTickersKey => MarketDataConstants.Redis.ActiveTickersKey;

    private static readonly JsonSerializerSettings SerializerSettings = new()
    {
        NullValueHandling    = NullValueHandling.Ignore,
        MissingMemberHandling = MissingMemberHandling.Ignore,
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
    };

    public RedisCacheService(IConnectionMultiplexer redis) => _db = redis.GetDatabase();

    public async Task SetCacheAsync<T>(string key, T data, int expirationMinutes)
    {
        var json = JsonConvert.SerializeObject(data, SerializerSettings);
        await _db.StringSetAsync(key, json, TimeSpan.FromMinutes(expirationMinutes));
    }

    public async Task<T?> GetCacheAsync<T>(string key)
    {
        var json = await _db.StringGetAsync(key);
        return json.HasValue ? JsonConvert.DeserializeObject<T>(json!, SerializerSettings) : default;
    }

    public async Task RemoveCacheAsync(string key) => await _db.KeyDeleteAsync(key);

    // ── Active ticker registry (fixes the hardcoded "testUser" bug) ──────────

    public async Task AddActiveTickerAsync(string ticker) =>
        await _db.SetAddAsync(ActiveTickersKey, ticker);

    public async Task RemoveActiveTickerAsync(string ticker) =>
        await _db.SetRemoveAsync(ActiveTickersKey, ticker);

    public async Task<List<string>> GetActiveTickersAsync()
    {
        var members = await _db.SetMembersAsync(ActiveTickersKey);
        return members.Select(m => m.ToString()).ToList();
    }
}
