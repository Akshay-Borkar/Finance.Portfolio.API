namespace Finance.AgentService.Infrastructure.Clients;

public interface ISentimentApiClient
{
    Task<string> AnalyseSentimentAsync(string text, CancellationToken ct = default);
}
