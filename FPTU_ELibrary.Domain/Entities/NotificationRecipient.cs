using System.Text.Json.Serialization;

namespace FPTU_ELibrary.Domain.Entities;

public class NotificationRecipient
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
    public Notification Notification { get; set; } = null!;

    [JsonIgnore]
    public User Recipient { get; set; } = null!;
}
