using FPTU_ELibrary.Application.Hubs;

namespace FPTU_ELibrary.API.Extensions;

public static class HubMappingExtensions
{
    public static void MapApplicationHubs(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHub<NotificationHub>("/notificationHub");
        endpoints.MapHub<AccountHub>("/accountHub");
        endpoints.MapHub<AiHub>("/ai-hub");
        // Add hub endpoint if needed
    }
}