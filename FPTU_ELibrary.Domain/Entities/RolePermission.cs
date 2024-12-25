using System.Text.Json.Serialization;
using FPTU_ELibrary.Domain.Interfaces;

namespace FPTU_ELibrary.Domain.Entities;

public class RolePermission : IAuditableEntity
{
    public int RolePermissionId { get; set; }
    public int RoleId { get; set; }
    public int FeatureId { get; set; }
    public int PermissionId { get; set; }

    // Datetime and employee who create or update the role permission
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = null!;
    public string? UpdatedBy { get; set; } 
    
    [JsonIgnore]
    public SystemRole Role { get; set; } = null!;
    
    [JsonIgnore]
    public SystemFeature Feature { get; set; } = null!;
    
    [JsonIgnore]
    public SystemPermission Permission { get; set; } = null!;
}