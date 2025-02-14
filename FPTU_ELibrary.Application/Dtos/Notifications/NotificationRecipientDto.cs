using System.Text.Json.Serialization;

namespace FPTU_ELibrary.Application.Dtos.Notifications;

public class NotificationRecipientDto
{
    // Key
    public int NotificationRecipientId { get; set; }

    // For specific notification
    public int NotificationId { get; set; }

    // Who recieve notification
    public Guid RecipientId { get; set; }

    // Mark as user is read or not
    public bool IsRead { get; set; }
    
    [JsonIgnore]
    public NotificationDto Notification { get; set; } = null!;
    
    [JsonIgnore]
    public UserDto Recipient { get; set; } = null!;   
}