using System.Text.Json.Serialization;

namespace FPTU_ELibrary.Domain.Entities;

public class SystemRole
{
    // Key
    public int RoleId { get; set; }
    
    // Role detail
    public string VietnameseName { get; set; } = null!;
    public string EnglishName { get; set; } = null!;

    // Mapping entities
    [JsonIgnore]
    public ICollection<User> Users { get; set; } = new List<User>();
}
