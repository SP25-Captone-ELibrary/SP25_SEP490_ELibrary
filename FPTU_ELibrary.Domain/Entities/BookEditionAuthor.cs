using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FPTU_ELibrary.Domain.Entities;

public class BookEditionAuthor
{
    // Key
    public int BookEditionAuthorId { get; set; }
    // Book belongs to 
    public int BookEditionId { get; set; }
    // Author belongs to
    public int AuthorId { get; set; }
    
    // Mapping entities
    public Author Author { get; set; } = null!;

    [JsonIgnore]
    public BookEdition BookEdition { get; set; } = null!;
}
