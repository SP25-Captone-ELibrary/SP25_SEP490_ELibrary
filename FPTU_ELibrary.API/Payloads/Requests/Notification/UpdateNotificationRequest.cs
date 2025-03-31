using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.API.Payloads.Requests.Notification;

public class UpdateNotificationRequest
{
    public string Title { get; set; } = null!;
    public string Message { get; set; } = null!;
    public NotificationType NotificationType { get; set; } 
}