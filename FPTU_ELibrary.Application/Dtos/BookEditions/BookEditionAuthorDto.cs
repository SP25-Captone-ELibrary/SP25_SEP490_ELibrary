using System.Text.Json.Serialization;
using FPTU_ELibrary.Application.Dtos.Authors;
using FPTU_ELibrary.Domain.Interfaces;

namespace FPTU_ELibrary.Application.Dtos.BookEditions;

public class BookEditionAuthorDto : IAuditableEntity
{
    // Key
    public int BookEditionAuthorId { get; set; }
    // Book belongs to 
    public int BookEditionId { get; set; }
    // Author belongs to
    public int AuthorId { get; set; }
    
    // Creation & Update person, datetime
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = null!;
    public string? UpdatedBy { get; set; }
    
    // Mapping entities
    public AuthorDto Author { get; set; } = null!;

    [JsonIgnore]
    public BookEditionDto BookEdition { get; set; } = null!;
}