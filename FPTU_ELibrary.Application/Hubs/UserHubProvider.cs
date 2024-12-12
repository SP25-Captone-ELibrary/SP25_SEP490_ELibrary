using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace FPTU_ELibrary.Application.Hubs;

public class UserHubProvider :IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
         return connection.User?.FindFirst(ClaimTypes.Email)?.Value;
    }
}