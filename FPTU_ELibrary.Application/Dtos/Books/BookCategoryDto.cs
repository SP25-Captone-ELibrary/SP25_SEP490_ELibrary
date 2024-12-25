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
    
    // Mapping fields
    [JsonIgnore]
    public BookDto Book { get; set; } = null!;
    
    public CategoryDto Category { get; set; } = null!;
}