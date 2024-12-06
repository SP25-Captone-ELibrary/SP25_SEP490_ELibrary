using System.Text.Json.Serialization;

namespace FPTU_ELibrary.Application.Dtos;

public class RolePermissionDto
{
    public int RolePermissionId { get; set; }
    public int RoleId { get; set; }
    public int FeatureId { get; set; }
    public int PermissionId { get; set; }

    public SystemRoleDto Role { get; set; } = null!;
    public SystemFeatureDto Feature { get; set; } = null!;
    public SystemPermissionDto Permission { get; set; } = null!;
}