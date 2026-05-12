using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Finance.NotificationService.Infrastructure.Hubs;

[Authorize]
public class StockPriceHub : Hub
{
    public async Task SubscribeToStock(string ticker)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, ticker);
    }

    public async Task UnsubscribeFromStock(string ticker)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, ticker);
    }
}
