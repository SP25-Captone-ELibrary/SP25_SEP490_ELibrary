using Microsoft.AspNetCore.SignalR;

namespace FPTU_ELibrary.Application.Hubs;

public class PaymentHub : Hub
{
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }
}