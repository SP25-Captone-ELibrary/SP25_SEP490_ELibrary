using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Domain.Specifications.Params;

public class LibraryItemSpecParams : BaseSpecParams
{
    // Book edition properties
    public bool? CanBorrow { get; set; }
    public bool? IsDeleted { get; set; }
    public bool? IsTrained { get; set; }
    
    #region Basic search properties
    // Basic search
    public string? Title { get; set; }
    public string? Author { get; set; }
    public string? Isbn { get; set; }
    public string? ClassificationNumber { get; set; }
    public string? Genres { get; set; }
    public string? Publisher { get; set; }
    public string? TopicalTerms { get; set; }
    #endregion
    
    // Filter fields
    // Field names: BookCode, Category, EditionTitle, PageCount
    // Language, PublicationYear, Format, Publisher, Isbn, EstimatedPrice, ShelfNumber, Barcode, CopyStatus
    public string[]? F { get; set; } 
    public FilterOperator[]? O { get; set; } // Operators
    public string[]? V { get; set; } // Values
    
    // Search type
    public SearchType SearchType { get; set; }
}
