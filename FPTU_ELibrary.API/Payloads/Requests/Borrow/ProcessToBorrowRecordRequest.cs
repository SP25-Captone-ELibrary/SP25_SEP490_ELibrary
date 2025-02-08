namespace FPTU_ELibrary.API.Payloads.Requests.Borrow;

public class ProcessToBorrowRecordRequest
{
    public int BorrowRequestId { get; set; }
    public Guid LibraryCardId { get; set; }
    public string BorrowCondition { get; set; } = null!;
    public List<int> LibraryItemInstanceIds { get; set; } = new();
}