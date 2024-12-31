using System.Text.Json.Serialization;
using FPTU_ELibrary.Domain.Interfaces;

namespace FPTU_ELibrary.Domain.Entities;

public class BookCategory : IAuditableEntity
{
    // Key
    public int BookCategoryId { get; set; }
    
    // BookId
    public int BookId { get; set; }
    // CategoryId
    public int CategoryId { get; set; }
    
    // Creation & Update person, datetime
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = null!;
    public string? UpdatedBy { get; set; }
    
    // Mapping fields
    [JsonIgnore]
    public Book Book { get; set; } = null!;
    
    [JsonIgnore]
    public Category Category { get; set; } = null!;
}