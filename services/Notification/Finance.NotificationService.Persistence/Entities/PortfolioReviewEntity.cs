namespace Finance.NotificationService.Persistence.Entities;

public class PortfolioReviewEntity
{
    public Guid Id { get; set; }
    public Guid ReviewId { get; set; }
    public Guid UserId { get; set; }
    public string NewsSummary { get; set; } = string.Empty;
    public string RiskAnalysis { get; set; } = string.Empty;
    public string WeeklyRecommendation { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
