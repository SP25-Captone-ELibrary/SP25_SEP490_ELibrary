using System.Text.Json.Serialization;

namespace FPTU_ELibrary.Domain.Entities;

public class RolePermission
{
    public int RolePermissionId { get; set; }
    public int RoleId { get; set; }
    public int FeatureId { get; set; }
    public int PermissionId { get; set; }

    [JsonIgnore]
    public SystemRole Role { get; set; } = null!;
    
    [JsonIgnore]
    public SystemFeature Feature { get; set; } = null!;
    
    [JsonIgnore]
    public SystemPermission Permission { get; set; } = null!;
}