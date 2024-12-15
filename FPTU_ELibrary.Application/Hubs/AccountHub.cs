using Microsoft.AspNetCore.SignalR;

namespace FPTU_ELibrary.Application.Hubs;

public class AccountHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        // await base.OnConnectedAsync();
        await Clients.All.SendAsync("ReceiveAccountMessage", "Connect successfully");
    }   
}