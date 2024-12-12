using FPTU_ELibrary.Application.Dtos;

namespace FPTU_ELibrary.API.Payloads.Requests.Notification;

public class CreateNotificationRequest
{
    public string Title { get; set; } = null!;
    public string Message { get; set; } = null!;
    public bool IsPublic { get; set; }
    public string CreateBy { get; set; }
    public string NotificationType { get; set; }
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
            CreatedBy = req.CreateBy,
            CreateDate = DateTime.Now ,
            NotificationType = req.NotificationType
        };
    }
}