using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.API.Payloads.Requests.Borrow;

public class CreateBorrowRecordRequest
{
    public Guid LibraryCardId { get; set; }
    public List<BorrowRecordDetailRequest> BorrowRecordDetails { get; set; } = new();
    public BorrowType BorrowType { get; set; }
    public DateTime? DueDate { get; set; } // Only handle due date from request when borrow type is in-library
}
