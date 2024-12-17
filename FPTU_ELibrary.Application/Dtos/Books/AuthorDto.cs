using Newtonsoft.Json;

namespace FPTU_ELibrary.Application.Dtos.Books;

public class AuthorDto
{
    // Key
    public int AuthorId { get; set; }

    // Author detail information
    public string? AuthorCode { get; set; }
    public string? AuthorImage { get; set; }
    public string? Email { get; set; } 
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string? Biography { get; set; } // Save as HTML text
    public DateTime? Dob { get; set; }
    public DateTime? DateOfDeath { get; set; } 
    public string? Nationality { get; set; } 
    
    // Creation and update datetime
    public DateTime CreateDate { get; set; }
    public DateTime? UpdateDate { get; set; }
    
    // Soft delete
    public bool IsDeleted { get; set; }

    // Mapping entity
    public ICollection<BookAuthorDto> BookAuthors { get; set; } = new List<BookAuthorDto>();
}