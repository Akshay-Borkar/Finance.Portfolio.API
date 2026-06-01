using Finance.Contracts.Events;
using Finance.NotificationService.Infrastructure.Constants;
using Finance.NotificationService.Infrastructure.Hubs;
using Finance.NotificationService.Persistence.DatabaseContext;
using Finance.NotificationService.Persistence.Entities;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Finance.NotificationService.Infrastructure.Consumers;

public class PortfolioReviewCompletedConsumer : IConsumer<PortfolioReviewCompleted>
{
    private readonly IHubContext<PortfolioReviewHub> _hub;
    private readonly NotificationDbContext _db;
    private readonly ILogger<PortfolioReviewCompletedConsumer> _logger;

    public PortfolioReviewCompletedConsumer(
        IHubContext<PortfolioReviewHub> hub,
        NotificationDbContext db,
        ILogger<PortfolioReviewCompletedConsumer> logger)
    {
        _hub = hub;
        _db = db;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PortfolioReviewCompleted> context)
    {
        var msg = context.Message;
        var ct = context.CancellationToken;

        _logger.LogInformation(
            "PortfolioReviewCompletedConsumer received ReviewId={ReviewId} UserId={UserId} HasNews={HasNews} HasRisk={HasRisk} HasRec={HasRec}",
            msg.ReviewId, msg.UserId,
            !string.IsNullOrWhiteSpace(msg.NewsSummary),
            !string.IsNullOrWhiteSpace(msg.RiskAnalysis),
            !string.IsNullOrWhiteSpace(msg.WeeklyRecommendation));

        // Persist to NotificationDB.
        var entity = new PortfolioReviewEntity
        {
            Id = Guid.NewGuid(),
            ReviewId = msg.ReviewId,
            UserId = msg.UserId,
            NewsSummary = msg.NewsSummary,
            RiskAnalysis = msg.RiskAnalysis,
            WeeklyRecommendation = msg.WeeklyRecommendation,
            GeneratedAt = msg.GeneratedAt,
            CreatedAt = DateTime.UtcNow
        };

        _db.PortfolioReviews.Add(entity);
        await _db.SaveChangesAsync(ct);

        // Push to the requesting user's SignalR group.
        var notification = new
        {
            reviewId = msg.ReviewId,
            userId = msg.UserId,
            newsSummary = msg.NewsSummary,
            riskAnalysis = msg.RiskAnalysis,
            weeklyRecommendation = msg.WeeklyRecommendation,
            generatedAt = msg.GeneratedAt
        };

        var group = $"portfolio-review-{msg.UserId}";
        _logger.LogInformation("Pushing SignalR event {Method} to group {Group}", NotificationConstants.SignalRMethods.ReceivePortfolioReview, group);

        await _hub.Clients
            .Group(group)
            .SendAsync(NotificationConstants.SignalRMethods.ReceivePortfolioReview, notification, ct);

        _logger.LogInformation(
            "Portfolio review {ReviewId} stored and pushed to group {Group}",
            msg.ReviewId, group);
    }
}
