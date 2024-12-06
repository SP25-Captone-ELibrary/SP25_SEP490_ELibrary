namespace FPTU_ELibrary.Domain.Entities;

public class RolePermission
{
    public int RolePermissionId { get; set; }
    public int RoleId { get; set; }
    public int FeatureId { get; set; }
    public int PermissionId { get; set; }

    public SystemRole Role { get; set; } = null!;
    public SystemFeature Feature { get; set; } = null!;
    public SystemPermission Permission { get; set; } = null!;
}