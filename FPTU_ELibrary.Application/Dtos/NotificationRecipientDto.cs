namespace FPTU_ELibrary.Application.Dtos;

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
    public NotificationDto NotificationDto { get; set; } = null!;
    public UserDto RecipientDto { get; set; } = null!;   
}