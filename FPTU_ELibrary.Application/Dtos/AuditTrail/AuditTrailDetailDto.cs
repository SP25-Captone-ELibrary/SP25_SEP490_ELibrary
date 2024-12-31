namespace FPTU_ELibrary.Application.Dtos.AuditTrail;

public class AuditTrailDetailDto
{
    // Json
    public object? OldValue { get; set; } = null!;
    // Json
    public object? NewValue { get; set; } = null!;
}