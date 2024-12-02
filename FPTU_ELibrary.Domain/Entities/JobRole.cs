using System.Text.Json.Serialization;
using FPTU_ELibrary.Domain.Entities.Base;

namespace FPTU_ELibrary.Domain.Entities;

public class JobRole : BaseRole
{
    // Key
    public int JobRoleId { get; set; }

    // Mapping entity
    [JsonIgnore]
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
