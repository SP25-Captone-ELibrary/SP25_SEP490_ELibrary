namespace FPTU_ELibrary.API.Payloads.Requests.Borrow;

public class CreateBorrowRecordRequest
{
    public Guid LibraryCardId { get; set; }
    public List<BorrowRecordDetailRequest> BorrowRecordDetails { get; set; } = new();
}