using System.Text.Json.Serialization;

namespace FPTU_ELibrary.Application.Dtos.Books;

public class BookCategoryDto
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
    public BookDto Book { get; set; } = null!;
    
    public CategoryDto Category { get; set; } = null!;
}