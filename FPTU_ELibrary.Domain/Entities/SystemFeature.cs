using System.Text.Json.Serialization;

namespace FPTU_ELibrary.Domain.Entities;

public class SystemFeature
{
    public int FeatureId { get; set; }
    public string VietnameseName { get; set; } = null!;
    public string EnglishName { get; set; } = null!;
    
    [JsonIgnore]
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}