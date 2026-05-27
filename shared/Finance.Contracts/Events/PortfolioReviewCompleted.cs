namespace Finance.Contracts.Events;

public record PortfolioReviewCompleted(
    Guid ReviewId,
    Guid UserId,
    string NewsSummary,
    string RiskAnalysis,
    string WeeklyRecommendation,
    DateTime GeneratedAt);
