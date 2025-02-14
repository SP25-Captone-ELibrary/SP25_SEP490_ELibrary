using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Domain.Specifications.Params;

public class BorrowRequestSpecParams : BaseSpecParams
{
    public BorrowRequestStatus? Status { get; set; }
    
    public DateTime?[]? RequestDateRange { get; set; }
    public DateTime?[]? ExpirationDateRange { get; set; }
    public DateTime?[]? CancelledAtRange { get; set; }
    
    // Fields:
    // LibraryCard (text)
    // Title (text)
    // ISBN (multi-text) - combined with ','
    // ClassificationNumber(DDC) (text)
    // CutterNumber (text)
    // Genres (multi-text) - combined with ','
    // TopicalTerms (multi-text) - combined with ','
    // Category (multi-text) - combined with ','
    // ShelfNumber (multi-text) - combined with ','
    // Barcode (multi-text) - combined with ','
    public string[]? F { get; set; } 
    public FilterOperator[]? O { get; set; } // Operators
    public string[]? V { get; set; } // Values
}