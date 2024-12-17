using Microsoft.AspNetCore.SignalR;
using Serilog;

namespace FPTU_ELibrary.Application.Hubs;

public class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        // await base.OnConnectedAsync();
        await Clients.All.SendAsync("ReceiveMsg", "Connect successfully");
    }

    // Client ngắt kết nối
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }
}