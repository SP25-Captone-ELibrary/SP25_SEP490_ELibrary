using System.Text.Json.Serialization;

namespace FPTU_ELibrary.Domain.Entities;

public class LibraryItemResource
{
    public int LibraryItemResourceId { get; set; }
    public int LibraryItemId { get; set; }
    public int ResourceId { get; set; }
    
    [JsonIgnore]
    public LibraryItem LibraryItem { get; set; } = null!;
    public LibraryResource LibraryResource { get; set; } = null!;
}