using FPTU_ELibrary.Application.Dtos.Notifications;

namespace FPTU_ELibrary.Application.Dtos.LibraryCard;

public class LibraryCardHolderNotificationRecipientDto
{
    // Key
    public int NotificationRecipientId { get; set; }

    // For specific notification
    public int NotificationId { get; set; }

    // Who recieve notification
    public Guid RecipientId { get; set; }

    // Mark as user is read or not
    public bool IsRead { get; set; }

    public NotificationDto Notification { get; set; } = null!;
}

public static class LibraryCardHolderNotificationRecipientDtoExtensions
{
    public static LibraryCardHolderNotificationRecipientDto ToCardHolderNotiRecipientDto(
        this NotificationRecipientDto dto)
    {
        return new()
        {
            NotificationRecipientId = dto.NotificationRecipientId,
            NotificationId = dto.NotificationId,
            RecipientId = dto.RecipientId,
            IsRead = dto.IsRead,
            Notification = new NotificationDto()
            {
                NotificationId = dto.NotificationId,
                Title = dto.Notification.Title,
                Message = dto.Notification.Message,
                IsPublic = dto.Notification.IsPublic,
                CreateDate = dto.Notification.CreateDate,
                CreatedBy = dto.Notification.CreatedBy,
                NotificationType = dto.Notification.NotificationType,
            },
        };
    }
}