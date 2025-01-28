using System.Text.Json.Serialization;
using FPTU_ELibrary.Domain.Interfaces;

namespace FPTU_ELibrary.Domain.Entities;

public class LibraryItemResource : IAuditableEntity
{
    public int LibraryItemResourceId { get; set; }
    public int LibraryItemId { get; set; }
    public int ResourceId { get; set; }
    
    // Creation, update datetime and employee who in charge of creation
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = null!;
    public string? UpdatedBy { get; set; }
    
    [JsonIgnore]
    public LibraryItem LibraryItem { get; set; } = null!;
    public LibraryResource LibraryResource { get; set; } = null!;
}