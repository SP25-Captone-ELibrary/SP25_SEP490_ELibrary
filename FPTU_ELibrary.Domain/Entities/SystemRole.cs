using System.Text.Json.Serialization;
using FPTU_ELibrary.Domain.Entities.Base;
using FPTU_ELibrary.Domain.Interfaces;

namespace FPTU_ELibrary.Domain.Entities;

public class SystemRole : BaseRole, IAuditableEntity
{
    // Key
    public int RoleId { get; set; }
    
    // Datetime and employee who create or update the role
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = null!;
    public string? UpdatedBy { get; set; } 
    
    // Mapping entities
    [JsonIgnore]
    public ICollection<User> Users { get; set; } = new List<User>();
    
    [JsonIgnore]
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
    
    [JsonIgnore]
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
