namespace FPTU_ELibrary.Domain.Specifications.Params;

public class NotificationSpecParams : BaseSpecParams
{
    public string Title { get; set; } = null!;
    public string Message { get; set; } = null!;
    public string CreatedBy { get; set; }
    public string NotificationType { get; set; }
    public DateTime[]? CreateDateRange { get; set; } 
}