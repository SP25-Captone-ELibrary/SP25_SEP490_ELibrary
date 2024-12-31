using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using FPTU_ELibrary.Domain.Interfaces;

namespace FPTU_ELibrary.Domain.Entities;

public class BookEditionAuthor : IAuditableEntity
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
    public Author Author { get; set; } = null!;

    [JsonIgnore]
    public BookEdition BookEdition { get; set; } = null!;
}
