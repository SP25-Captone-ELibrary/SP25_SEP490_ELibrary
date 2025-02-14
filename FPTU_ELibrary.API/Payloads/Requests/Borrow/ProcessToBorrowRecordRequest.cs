namespace FPTU_ELibrary.API.Payloads.Requests.Borrow;

public class ProcessToBorrowRecordRequest
{
    public int BorrowRequestId { get; set; }
    public Guid LibraryCardId { get; set; }
    public List<BorrowRecordDetailRequest> BorrowRecordDetails { get; set; } = new();
}