namespace Finance.SentimentService.Infrastructure.Services;

public interface ISentimentAnalysisService
{
    string PredictSentiment(string text);
}
