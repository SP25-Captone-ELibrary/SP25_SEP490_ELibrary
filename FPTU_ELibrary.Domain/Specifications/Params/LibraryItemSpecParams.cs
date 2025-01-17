using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Domain.Specifications.Params;

public class LibraryItemSpecParams : BaseSpecParams
{
    // Book edition properties
    public bool? CanBorrow { get; set; }
    public bool? IsDeleted { get; set; }
    public bool? IsTrained { get; set; }
    
    // Filter fields
    // Field names: BookCode, Category, EditionTitle, PageCount
    // Language, PublicationYear, Format, Publisher, Isbn, EstimatedPrice, ShelfNumber, Barcode, CopyStatus
    public string[]? F { get; set; } 
    public FilterOperator[]? O { get; set; } // Operators
    public string[]? V { get; set; } // Values
}
