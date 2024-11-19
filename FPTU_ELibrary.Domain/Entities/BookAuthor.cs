using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FPTU_ELibrary.Domain.Entities;

public class BookAuthor
{
    // Key
    public int BookAuthorId { get; set; }
    // Book belongs to 
    public int BookId { get; set; }
    // Author belongs to
    public int AuthorId { get; set; }
    
    // Mapping entities
    public Author Author { get; set; } = null!;

    [JsonIgnore]
    public Book Book { get; set; } = null!;
}
