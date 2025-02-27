using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Domain.Specifications.Params;

public class NotificationSpecParams : BaseSpecParams
{
    public bool? IsPublic { get; set; }
    public string? Email { get; set; }
    public Guid? CreatedBy { get; set; } = null!;
    public NotificationType? NotificationType { get; set; }
    public DateTime?[]? CreateDateRange { get; set; } 
}