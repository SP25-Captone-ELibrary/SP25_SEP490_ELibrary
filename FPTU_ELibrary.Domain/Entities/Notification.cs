using System.Text.Json.Serialization;

namespace FPTU_ELibrary.Domain.Entities;

public class Notification
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
    // notification type 
    public string NotificationType { get; set; } = null!;
    public ICollection<NotificationRecipient> NotificationRecipients { get; set; } = new List<NotificationRecipient>();
}
