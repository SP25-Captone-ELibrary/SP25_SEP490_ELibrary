using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Domain.Specifications.Params;

public class BookEditionSpecParams : BaseSpecParams
{
    // Search fields: Title, Summary, EditionTitle, EditionNumber, PublicationYear, Language, Isbn
    
    // Book properties
    public DateTime?[]? CreatedAtRange { get; set; } 
    public DateTime?[]? UpdatedAtRange { get; set; } 
    
    // Book edition properties
    public int[]? EditionNumberRange { get; set; }
    public int[]? PublicationYearRange { get; set; }
    public int[]? PageCountRange { get; set; }
    public string? Format { get; set; }
    public string? Language { get; set; }
    public bool? CanBorrow { get; set; }
    public bool? IsDeleted { get; set; }
    public BookEditionStatus? Status { get; set; } // Draft/Published
    
    // Book edition author properties
    public string? AuthorCode { get; set; }
    public string? AuthorFullName { get; set; }
    public DateTime?[]? AuthorDobRange { get; set; }
    public DateTime?[]? AuthorDateOfDeathRange { get; set; }
    public string? AuthorNationality { get; set; }
    
    // Book edition copy properties
    public int? ShelfId { get; set; }
    public string? BookEditionCopyCode { get; set; }
    public string? BookEditionCopyStatus { get; set; }
}