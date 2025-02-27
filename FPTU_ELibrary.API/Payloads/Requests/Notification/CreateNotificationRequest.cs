using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.Notifications;
using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.API.Payloads.Requests.Notification;

public class CreateNotificationRequest
{
    public string Title { get; set; } = null!;
    public string Message { get; set; } = null!;
    public bool IsPublic { get; set; }
    public NotificationType NotificationType { get; set; } 
    public List<string>? ListRecipient { get; set; }
}

public static class CreateNotificationRequestExtension
{
    public static NotificationDto ToNotificationDto(this CreateNotificationRequest req)
    {
        return new NotificationDto()
        {
            IsPublic = req.IsPublic,
            Message = req.Message,
            Title = req.Title,
            NotificationType = req.NotificationType
        };
    }
}