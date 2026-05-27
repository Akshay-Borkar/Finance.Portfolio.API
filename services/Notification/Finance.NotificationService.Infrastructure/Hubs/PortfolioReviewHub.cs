using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Finance.NotificationService.Infrastructure.Hubs;

[Authorize]
public class PortfolioReviewHub(ILogger<PortfolioReviewHub> logger) : Hub
{
    public override async Task OnConnectedAsync()
    {
        // The JWT is issued with claim "uid" = user.Id (Guid). This must match what
        // PortfolioReviewCompletedConsumer uses to push: $"portfolio-review-{msg.UserId}".
        var userId = Context.User?.FindFirstValue("uid");

        if (!string.IsNullOrEmpty(userId))
        {
            var group = $"portfolio-review-{userId}";
            await Groups.AddToGroupAsync(Context.ConnectionId, group);
            logger.LogInformation("SignalR: connection {ConnectionId} joined group {Group}", Context.ConnectionId, group);
        }
        else
        {
            logger.LogWarning("SignalR: connection {ConnectionId} has no 'uid' claim — user will not receive portfolio review notifications", Context.ConnectionId);
        }

        await base.OnConnectedAsync();
    }
}
