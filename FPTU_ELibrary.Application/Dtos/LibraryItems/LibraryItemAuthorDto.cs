using System.Text.Json.Serialization;
using FPTU_ELibrary.Application.Dtos.Authors;
using FPTU_ELibrary.Domain.Interfaces;

namespace FPTU_ELibrary.Application.Dtos.LibraryItems;

public class LibraryItemAuthorDto : IAuditableEntity
{
    // Key
    public int LibraryItemAuthorId { get; set; }
    // Book belongs to 
    public int LibraryItemId { get; set; }
    // Author belongs to
    public int AuthorId { get; set; } // Main author only [100b]
    
    // Creation & Update person, datetime
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = null!;
    public string? UpdatedBy { get; set; }
    
    // Mapping entities
    public AuthorDto Author { get; set; } = null!;

    [JsonIgnore]
    public LibraryItemDto LibraryItem { get; set; } = null!;
}