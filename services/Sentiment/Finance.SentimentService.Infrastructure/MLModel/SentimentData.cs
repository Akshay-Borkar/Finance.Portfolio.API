using Microsoft.ML.Data;

namespace Finance.SentimentService.Infrastructure.MLModel;

public class SentimentData
{
    [LoadColumn(0)]
    [ColumnName("Label")]
    public bool Sentiment { get; set; }

    [LoadColumn(1)]
    [ColumnName("Text")]
    public string Text { get; set; } = string.Empty;
}

public class SentimentPrediction : SentimentData
{
    [ColumnName("PredictedLabel")]
    public bool Prediction { get; set; }

    public float Probability { get; set; }

    public float Score { get; set; }
}
