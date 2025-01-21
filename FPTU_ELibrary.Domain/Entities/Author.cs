using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FPTU_ELibrary.Domain.Entities;

public class Author
{
    // Key
    public int AuthorId { get; set; }

    // Author detail information
    public string AuthorCode { get; set; } = null!;
    public string? AuthorImage { get; set; }
    public string FullName { get; set; } = null!;
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
    [JsonIgnore]
    public ICollection<LibraryItemAuthor> LibraryItemAuthors { get; set; } = new List<LibraryItemAuthor>();
}
