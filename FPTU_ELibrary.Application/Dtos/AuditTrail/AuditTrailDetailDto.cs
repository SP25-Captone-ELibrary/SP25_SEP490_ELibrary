namespace FPTU_ELibrary.Application.Dtos.AuditTrail;

public class AuditTrailDetailDto
{
    // Json
    public object? OldValues { get; set; } = null!;
    // Json
    public object? NewValues { get; set; } = null!;
}