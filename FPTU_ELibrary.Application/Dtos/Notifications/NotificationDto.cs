namespace FPTU_ELibrary.Application.Dtos.Notifications;

public class NotificationDto
{
    
    // Key
    public int NotificationId { get; set; }
    // Notification detail
    public string Title { get; set; } = null!;
    public string Message { get; set; } = null!;
    public bool IsPublic { get; set; }
    // Creation datetime and employee
    public DateTime CreateDate { get; set; }
    public string CreatedBy { get; set; } = null!;
    // Notification Type
    public string NotificationType { get; set; } = null!;
    
    public ICollection<NotificationRecipientDto> NotificationRecipients { get; set; } = new List<NotificationRecipientDto>();
}