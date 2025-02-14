using FPTU_ELibrary.API.Payloads.Requests.Borrow;

namespace FPTU_ELibrary.API.Payloads.Requests.LibraryCard;

public class SelfCheckoutBorrowRequest
{
    public Guid LibraryCardId { get; set; }
    public List<BorrowRecordDetailRequest> BorrowRecordDetails { get; set; } = new();
}