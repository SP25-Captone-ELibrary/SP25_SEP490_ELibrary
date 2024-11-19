using System.Text.Json.Serialization;

namespace FPTU_ELibrary.Domain.Entities;

public class JobRole
{
    // Key
    public int JobRoleId { get; set; }

    // Job role detail
    public string EnglishName { get; set; } = null!;
    public string VietnameseName { get; set; } = null!;

    // Mapping entity
    [JsonIgnore]
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
