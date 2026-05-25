using System.Text.Json.Serialization;

namespace Finance.SentimentService.Infrastructure.Services;

public class MarketAuxResponse
{
    [JsonPropertyName("data")]
    public List<MarketAuxArticle> Data { get; set; } = [];
}

public class MarketAuxArticle
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}
