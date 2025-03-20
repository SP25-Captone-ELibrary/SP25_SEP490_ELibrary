using System.Text.Json.Serialization;

namespace FPTU_ELibrary.Application.Dtos.LibraryItems;

public class LibraryResourceUrlDto
{
    // Key
    public int LibraryResourceUrlId { get; set; }
    // Detail Information
    public int PartNumber { get; set; }
    public string Url { get; set; } = null!;

    // Foreign Key
    public int LibraryResourceId { get; set; }
    
    [JsonIgnore]
    public LibraryResourceDto LibraryResource { get; set; } = null!;
}