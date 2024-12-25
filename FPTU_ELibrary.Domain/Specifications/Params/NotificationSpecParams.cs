namespace FPTU_ELibrary.Domain.Specifications.Params;

public class NotificationSpecParams : BaseSpecParams
{
    public string Title { get; set; } = null!;
    public string Message { get; set; } = null!;
    public string CreatedBy { get; set; } = null!;
    public string NotificationType { get; set; } = null!;
    public DateTime[]? CreateDateRange { get; set; } 
}