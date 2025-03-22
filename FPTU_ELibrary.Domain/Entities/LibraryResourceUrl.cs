using System.Text.Json.Serialization;
using FPTU_ELibrary.Domain.Interfaces;

namespace FPTU_ELibrary.Domain.Entities;

public class LibraryResourceUrl 
{
    // Key
    public int LibraryResourceUrlId { get; set; }
    // Detail Information
    public int PartNumber { get; set; }
    public string Url { get; set; } = null!;

    // Foreign Key
    public int LibraryResourceId { get; set; }
    
    [JsonIgnore]
    public LibraryResource LibraryResource { get; set; } = null!;
}