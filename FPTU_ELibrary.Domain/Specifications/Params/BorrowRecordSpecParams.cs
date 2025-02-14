using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Domain.Specifications.Params;

public class BorrowRecordSpecParams : BaseSpecParams
{
    public BorrowRecordStatus? Status { get; set; }
    public bool? SelfServiceBorrow { get; set; }
    
    public DateTime?[]? BorrowDateRange { get; set; }
    public DateTime?[]? DueDateRange { get; set; }
    public DateTime?[]? ReturnDateRange { get; set; }

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