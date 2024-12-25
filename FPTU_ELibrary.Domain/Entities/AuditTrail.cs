using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Domain.Entities;

public class AuditTrail
{
    public int AuditTrailId { get; set; }
    
    public string Email { get; set; } = null!;
    
    public string? EntityId { get; set; } 
    
    public string EntityName { get; set; } = null!;
    
    public TrailType TrailType { get; set; }
    
    public DateTime DateUtc { get; set; }
    
    public Dictionary<string, object?> OldValues { get; set; } = [];
    
    public Dictionary<string, object?> NewValues { get; set; } = [];
    
    public List<string> ChangedColumns { get; set; } = [];
}