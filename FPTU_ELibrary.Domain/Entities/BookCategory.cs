using System.Text.Json.Serialization;

namespace FPTU_ELibrary.Domain.Entities;

public class BookCategory
{
    // Key
    public int BookCategoryId { get; set; }
    
    // BookId
    public int BookId { get; set; }
    // CategoryId
    public int CategoryId { get; set; }
    
    // Mapping fields
    [JsonIgnore]
    public Book Book { get; set; } = null!;
    
    [JsonIgnore]
    public Category Category { get; set; } = null!;
}