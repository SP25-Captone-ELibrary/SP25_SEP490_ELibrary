using System.Text.Json.Serialization;
using FPTU_ELibrary.Domain.Entities.Base;

namespace FPTU_ELibrary.Domain.Entities;

public class SystemRole : BaseRole
{
    // Key
    public int RoleId { get; set; }

    // Mapping entities
    [JsonIgnore]
    public ICollection<User> Users { get; set; } = new List<User>();
    
    [JsonIgnore]
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
    
    [JsonIgnore]
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
