namespace Finance.SentimentService.Infrastructure.Services;

// ML.NET pipeline is stubbed — model training data (sentimentdata.csv) is not yet
// available. Replace the PredictSentiment body with the commented block below once
// the model file is added to the project.
public class SentimentAnalysisService : ISentimentAnalysisService
{
    public string PredictSentiment(string text)
    {
        // TODO: restore when ML model is ready
        // var input = new SentimentData { NewsText = text };
        // var prediction = _predictionEngine.Predict(input);
        // return _sentimentLabels[prediction.Prediction] ?? "Unknown";
        return "Unknown";
    }
}
