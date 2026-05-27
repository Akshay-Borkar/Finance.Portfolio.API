using System.ComponentModel;
using Finance.AgentService.Infrastructure.Clients;
using Microsoft.SemanticKernel;

namespace Finance.AgentService.Infrastructure.Plugins;

public sealed class SentimentKernelPlugin(ISentimentApiClient sentimentClient)
{
    [KernelFunction("analyze_sentiment")]
    [Description("Analyses the sentiment of a text string and returns Positive, Negative, or Neutral")]
    public async Task<string> AnalyzeSentimentAsync(
        [Description("The text to analyse for sentiment")] string text,
        CancellationToken cancellationToken = default)
    {
        return await sentimentClient.AnalyseSentimentAsync(text, cancellationToken);
    }
}
