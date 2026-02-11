using Microsoft.AspNetCore.SignalR;

namespace BrokerSystem.Api.Infrastructure.Hubs;

public class BrokerHub : Hub
{
    public async Task SendNotification(string title, string message)
    {
        await Clients.All.SendAsync("ReceiveNotification", title, message);
    }
}
