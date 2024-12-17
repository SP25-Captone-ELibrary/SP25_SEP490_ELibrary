using Newtonsoft.Json;

namespace FPTU_ELibrary.Application.Dtos.Books;

public class BookAuthorDto
{
    // Key
    public int BookAuthorId { get; set; }
    // Book belongs to 
    public int BookId { get; set; }
    // Author belongs to
    public int AuthorId { get; set; }
    
    // Mapping entities
    [JsonIgnore]
    public AuthorDto Author { get; set; } = null!;

    [JsonIgnore]
    public BookDto Book { get; set; } = null!;
}