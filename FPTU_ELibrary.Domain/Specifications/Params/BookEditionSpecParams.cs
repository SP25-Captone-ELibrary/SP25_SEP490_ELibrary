namespace FPTU_ELibrary.Domain.Specifications.Params;

public class BookEditionSpecParams : BaseSpecParams
{
    // Search fields: Title, Summary, EditionTitle, EditionNumber, PublicationYear, Language, Isbn
    
    // Book properties
    public bool? IsDraft { get; set; }
    public bool? IsDeleted { get; set; }
    public Guid? CreateBy { get; set; }
    public DateTime[]? CreateDateRange { get; set; } 
    public DateTime[]? ModifiedDateRange { get; set; } 
    
    // Book edition properties
    public int[]? EditionNumberRange { get; set; }
    public int[]? PublicationYearRange { get; set; }
    public int[]? PageCountRange { get; set; }
    public string? Format { get; set; }
    public string? Language { get; set; }
    public bool? CanBorrow { get; set; }
    
    // Book edition author properties
    public string? AuthorCode { get; set; }
    public string? AuthorFullName { get; set; }
    public DateTime[]? AuthorDobRange { get; set; }
    public DateTime[]? AuthorDateOfDateRange { get; set; }
    public string? AuthorNationality { get; set; }
    
    // Book edition copy properties
    public int? ShelfId { get; set; }
    public string? BookEditionCopyCode { get; set; }
    public string? BookEditionCopyStatus { get; set; }
}