#pragma warning disable SKEXP0110
#pragma warning disable SKEXP0001
using Finance.Contracts.Events;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Finance.AgentService.Infrastructure.Orchestration;

public sealed class PortfolioReviewOrchestrator : IPortfolioReviewOrchestrator
{
    private readonly IAgentCache _agentCache;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<PortfolioReviewOrchestrator> _logger;

    public PortfolioReviewOrchestrator(
        IAgentCache agentCache,
        IPublishEndpoint publishEndpoint,
        ILogger<PortfolioReviewOrchestrator> logger)
    {
        _agentCache = agentCache;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task<PortfolioReviewResult> RunAsync(Guid userId, string[] tickers, CancellationToken ct = default)
    {
        _logger.LogInformation("Starting portfolio review for user {UserId} with {Count} tickers", userId, tickers.Length);

        var agents = await _agentCache.GetOrCreateAsync(ct);

        var chat = new AgentGroupChat(
            agents.Orchestrator,
            agents.News,
            agents.Risk,
            agents.Report)
        {
            ExecutionSettings = new AgentGroupChatSettings
            {
                SelectionStrategy = new SequentialSelectionStrategy(),
                TerminationStrategy = new PortfolioReviewTerminationStrategy { MaximumIterations = 12 }
            }
        };

        chat.AddChatMessage(new ChatMessageContent(AuthorRole.User,
            $"Generate a weekly portfolio review for the following holdings: {string.Join(", ", tickers)}. " +
            $"UserId: {userId}. Today: {DateTime.UtcNow:yyyy-MM-dd}."));

        var messages = new List<ChatMessageContent>();
        await foreach (var msg in chat.InvokeAsync(ct))
        {
            var snippet = (msg.Content ?? "")[..Math.Min(120, msg.Content?.Length ?? 0)];
            _logger.LogInformation("[{Agent}] {Snippet}", msg.AuthorName, snippet);
            messages.Add(msg);
        }

        var result = ParseReport(messages, userId);

        _logger.LogInformation(
            "ParseReport complete: ReviewId={ReviewId} UserId={UserId} HasNews={HasNews} HasRisk={HasRisk} HasRec={HasRec}",
            result.ReviewId, result.UserId,
            !string.IsNullOrWhiteSpace(result.NewsSummary),
            !string.IsNullOrWhiteSpace(result.RiskAnalysis),
            !string.IsNullOrWhiteSpace(result.WeeklyRecommendation));

        try
        {
            _logger.LogInformation("Publishing PortfolioReviewCompleted for ReviewId={ReviewId} UserId={UserId}", result.ReviewId, result.UserId);
            await _publishEndpoint.Publish(new PortfolioReviewCompleted(
                result.ReviewId,
                result.UserId,
                result.NewsSummary,
                result.RiskAnalysis,
                result.WeeklyRecommendation,
                result.GeneratedAt), ct);

            _logger.LogInformation("Portfolio review {ReviewId} published for user {UserId}", result.ReviewId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish PortfolioReviewCompleted event for review {ReviewId}, user {UserId}. Review result is preserved.", result.ReviewId, userId);
        }
        return result;
    }

    private static PortfolioReviewResult ParseReport(IReadOnlyList<ChatMessageContent> messages, Guid userId)
    {
        var reportContent = messages
            .LastOrDefault(m => m.Role == AuthorRole.Assistant && m.AuthorName == AgentNames.Report)
            ?.Content ?? "";

        if (string.IsNullOrWhiteSpace(reportContent))
            reportContent = messages.LastOrDefault(m => m.Role == AuthorRole.Assistant)?.Content ?? "Report unavailable.";

        var newsSummary = ExtractSection(reportContent, "## News Summary", "## Risk Analysis");
        var riskAnalysis = ExtractSection(reportContent, "## Risk Analysis", "## Weekly Recommendation");
        var recommendation = ExtractSection(reportContent, "## Weekly Recommendation", "[REPORT_COMPLETE]");

        if (string.IsNullOrWhiteSpace(newsSummary))
            newsSummary = reportContent;

        return new PortfolioReviewResult(Guid.NewGuid(), userId, newsSummary, riskAnalysis, recommendation, DateTime.UtcNow);
    }

    private static string ExtractSection(string text, string startMarker, string endMarker)
    {
        var start = text.IndexOf(startMarker, StringComparison.OrdinalIgnoreCase);
        if (start < 0) return string.Empty;
        start += startMarker.Length;
        var end = text.IndexOf(endMarker, start, StringComparison.OrdinalIgnoreCase);
        return (end < 0 ? text[start..] : text[start..end]).Trim();
    }

    public const string OrchestratorAgentInstructions = """
        You are a portfolio review coordinator.
        When the review task begins, briefly acknowledge it and confirm the analysis team should proceed.
        When you see [REPORT_COMPLETE] in the conversation, respond with a brief confirmation and the marker [REVIEW_COMPLETE].
        Keep responses concise.
        """;

    public const string NewsAgentInstructions = """
        You are a financial news analyst specialising in Indian stock markets.
        For each ticker provided, call the fetch_news function to retrieve recent headlines.
        Compile a concise summary per holding. Format:

        ### [TICKER]
        - [Headline 1]
        - [Headline 2]

        If no news is found, note "No recent news available."
        """;

    public const string RiskAgentInstructions = """
        You are a portfolio risk assessment specialist.
        Based on the news headlines, call the analyze_sentiment function on key headlines.
        Categorise each holding as Low Risk, Medium Risk, or High Risk. Format:

        ### [TICKER] — [Risk Level]
        - Overall Sentiment: [Positive/Neutral/Negative]
        - Risk Factors: [brief description]
        """;

    public const string ReportAgentInstructions = """
        You are a senior portfolio analyst writing executive-level weekly reviews.
        Based on the news and risk analysis in this conversation, write a structured report using this exact format:

        ## News Summary
        [Comprehensive news highlights per holding]

        ## Risk Analysis
        [Risk categorisation and elevated-risk holdings]

        ## Weekly Recommendation
        [Specific, actionable recommendations]

        [REPORT_COMPLETE]
        """;
}
