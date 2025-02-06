using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Domain.Specifications.Params;

public class BorrowRequestSpecParams : BaseSpecParams
{
    public BorrowRequestStatus? Status { get; set; }
    
    public DateTime?[]? RequestDateRange { get; set; }
    public DateTime?[]? ExpirationDateRange { get; set; }
    public DateTime?[]? CancelledAtRange { get; set; }
}