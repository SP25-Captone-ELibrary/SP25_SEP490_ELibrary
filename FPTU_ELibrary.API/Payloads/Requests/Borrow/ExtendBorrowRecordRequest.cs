namespace FPTU_ELibrary.API.Payloads.Requests.Borrow;

public class ExtendBorrowRecordRequest
{
    public List<int> BorrowRecordDetailIds { get; set; } = new();
}