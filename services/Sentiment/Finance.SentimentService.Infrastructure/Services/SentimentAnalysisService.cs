using Finance.SentimentService.Infrastructure.MLModel;
using Microsoft.Extensions.Logging;
using Microsoft.ML;

namespace Finance.SentimentService.Infrastructure.Services;

public class SentimentAnalysisService : ISentimentAnalysisService
{
    private readonly PredictionEngine<SentimentData, SentimentPrediction> _predictionEngine;
    private readonly ILogger<SentimentAnalysisService> _logger;

    private static readonly string ModelPath = Path.Combine(
        AppContext.BaseDirectory, "MLModel", "sentiment_model.zip");

    private static readonly string DataPath = Path.Combine(
        AppContext.BaseDirectory, "MLModel", "sentiment_data.csv");

    public SentimentAnalysisService(ILogger<SentimentAnalysisService> logger)
    {
        _logger = logger;
        var mlContext = new MLContext(seed: 42);

        if (!File.Exists(ModelPath))
        {
            _logger.LogInformation(
                "Sentiment model not found at {Path}. Training now...", ModelPath);

            Directory.CreateDirectory(Path.GetDirectoryName(ModelPath)!);
            SentimentModelTrainer.TrainAndSave(DataPath, ModelPath);

            _logger.LogInformation("Sentiment model trained and saved successfully.");
        }

        var loadedModel = mlContext.Model.Load(ModelPath, out _);

        _predictionEngine = mlContext.Model.CreatePredictionEngine<SentimentData, SentimentPrediction>(loadedModel);

        _logger.LogInformation("Sentiment model loaded successfully from {Path}", ModelPath);
    }

    public string PredictSentiment(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "Neutral";

        try
        {
            var input = new SentimentData { Text = text };
            var prediction = _predictionEngine.Predict(input);

            if (prediction.Probability >= 0.6f)
                return "Positive";
            if (prediction.Probability <= 0.4f)
                return "Negative";

            return "Neutral";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error predicting sentiment for text: {Text}", text);
            return "Neutral";
        }
    }
}
