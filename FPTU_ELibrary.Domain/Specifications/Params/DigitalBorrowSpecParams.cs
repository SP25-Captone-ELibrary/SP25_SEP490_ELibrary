using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Domain.Specifications.Params;

public class DigitalBorrowSpecParams : BaseSpecParams
{
    public bool? IsExtended { get; set; }
    public BorrowDigitalStatus? Status { get; set; }
    
    public DateTime?[]? RegisterDateRange { get; set; }
    public DateTime?[]? ExpiryDateRange { get; set; }
    
    // Filter fields:
    //  ResourceType
    //  ResourceSize
    //  DefaultBorrowDurationDays
    //  BorrowPrice
    public string[]? F { get; set; } 
    public FilterOperator[]? O { get; set; } // Operators
    public string[]? V { get; set; } // Values
}