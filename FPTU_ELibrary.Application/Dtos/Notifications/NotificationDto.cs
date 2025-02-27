using FPTU_ELibrary.Application.Dtos.Employees;
using FPTU_ELibrary.Domain.Common.Enums;

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
    public Guid CreatedBy { get; set; } 
    
    // Notification Type
    public NotificationType NotificationType { get; set; }
    
    public EmployeeDto CreatedByNavigation { get; set; } = null!;
    public ICollection<NotificationRecipientDto> NotificationRecipients { get; set; } = new List<NotificationRecipientDto>();
}