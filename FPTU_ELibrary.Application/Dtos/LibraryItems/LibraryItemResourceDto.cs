using System.Text.Json.Serialization;

namespace FPTU_ELibrary.Application.Dtos.LibraryItems;

public class LibraryItemResourceDto
{
    public int LibraryItemResourceId { get; set; }
    public int LibraryItemId { get; set; }
    public int ResourceId { get; set; }
    
    [JsonIgnore]
    public LibraryItemDto LibraryItem { get; set; } = null!;
    public LibraryResourceDto LibraryResource { get; set; } = null!;
}