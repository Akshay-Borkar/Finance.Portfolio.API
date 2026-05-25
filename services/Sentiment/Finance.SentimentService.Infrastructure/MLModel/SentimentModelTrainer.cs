using Microsoft.ML;

namespace Finance.SentimentService.Infrastructure.MLModel;

public static class SentimentModelTrainer
{
    public static void TrainAndSave(string dataPath, string modelPath)
    {
        var mlContext = new MLContext(seed: 42);

        var dataView = mlContext.Data.LoadFromTextFile<SentimentData>(
            dataPath,
            hasHeader: true,
            separatorChar: ',');

        var splitData = mlContext.Data.TrainTestSplit(dataView, testFraction: 0.2);

        var pipeline = mlContext.Transforms.Text
            .FeaturizeText(
                outputColumnName: "Features",
                inputColumnName: nameof(SentimentData.Text))
            .Append(mlContext.BinaryClassification.Trainers.SdcaLogisticRegression(
                labelColumnName: "Label",
                featureColumnName: "Features"));

        var model = pipeline.Fit(splitData.TrainSet);

        var predictions = model.Transform(splitData.TestSet);
        var metrics = mlContext.BinaryClassification.Evaluate(
            predictions,
            labelColumnName: "Label");

        Console.WriteLine($"ML.NET Sentiment Model Trained:");
        Console.WriteLine($"  Accuracy:  {metrics.Accuracy:P2}");
        Console.WriteLine($"  AUC:       {metrics.AreaUnderRocCurve:P2}");
        Console.WriteLine($"  F1 Score:  {metrics.F1Score:P2}");

        mlContext.Model.Save(model, splitData.TrainSet.Schema, modelPath);
        Console.WriteLine($"  Model saved to: {modelPath}");
    }
}
