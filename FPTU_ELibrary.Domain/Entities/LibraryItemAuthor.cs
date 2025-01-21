using System.Text.Json.Serialization;
using FPTU_ELibrary.Domain.Interfaces;

namespace FPTU_ELibrary.Domain.Entities;

public class LibraryItemAuthor : IAuditableEntity
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
    public Author Author { get; set; } = null!;

    [JsonIgnore]
    public LibraryItem LibraryItem { get; set; } = null!;
}
