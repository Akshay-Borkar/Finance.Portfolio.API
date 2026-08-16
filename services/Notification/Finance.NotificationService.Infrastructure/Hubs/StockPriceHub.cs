using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Finance.NotificationService.Infrastructure.Hubs;

[Authorize]
public class StockPriceHub : Hub
{
    // Live subscriber counts per ticker, for the upcoming hub diagnostics endpoint.
    private static readonly ConcurrentDictionary<string, HashSet<string>> SubscriberConnections = new();

    public async Task SubscribeToStock(string ticker)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, ticker);

        var connections = SubscriberConnections.GetOrAdd(ticker, _ => new HashSet<string>());
        lock (connections)
        {
            connections.Add(Context.ConnectionId);
        }
    }

    public async Task UnsubscribeFromStock(string ticker)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, ticker);
    }

    public static int GetSubscriberCount(string ticker) =>
        SubscriberConnections.TryGetValue(ticker, out var connections) ? connections.Count : 0;
}
