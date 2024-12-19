using FPTU_ELibrary.Application.Dtos.Authors;
using Newtonsoft.Json;

namespace FPTU_ELibrary.Application.Dtos.Books;

public class BookEditionAuthorDto
{
    // Key
    public int BookEditionAuthorId { get; set; }
    // Book belongs to 
    public int BookEditionId { get; set; }
    // Author belongs to
    public int AuthorId { get; set; }
    
    // Mapping entities
    public AuthorDto Author { get; set; } = null!;

    [JsonIgnore]
    public BookEditionDto BookEdition { get; set; } = null!;
}