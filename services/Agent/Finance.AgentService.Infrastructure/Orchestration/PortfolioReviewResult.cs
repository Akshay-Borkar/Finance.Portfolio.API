namespace Finance.AgentService.Infrastructure.Orchestration;

public sealed record PortfolioReviewResult(
    Guid ReviewId,
    Guid UserId,
    string NewsSummary,
    string RiskAnalysis,
    string WeeklyRecommendation,
    DateTime GeneratedAt);
