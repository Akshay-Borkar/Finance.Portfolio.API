namespace Finance.AgentService.Infrastructure.Clients;

public interface IMarketAuxClient
{
    Task<List<string>> FetchLatestStockNewsAsync(string ticker, CancellationToken ct = default);
}
