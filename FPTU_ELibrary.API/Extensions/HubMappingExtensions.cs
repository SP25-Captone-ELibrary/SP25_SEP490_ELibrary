using FPTU_ELibrary.Application.Hubs;

namespace FPTU_ELibrary.API.Extensions;

public static class HubMappingExtensions
{
    public static void MapApplicationHubs(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHub<NotificationHub>("/notificationHub");
        endpoints.MapHub<AccountHub>("/accountHub");
        endpoints.MapHub<AiHub>("/ai-hub");
        endpoints.MapHub<PaymentHub>("/payment-hub");
        endpoints.MapHub<DigitalBorrowHub>("/digital-borrow-hub");
        // Add hub endpoint if needed
    }
}