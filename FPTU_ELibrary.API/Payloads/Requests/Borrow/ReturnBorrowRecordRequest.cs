using FPTU_ELibrary.API.Payloads.Requests.Fine;

namespace FPTU_ELibrary.API.Payloads.Requests.Borrow;

public class ReturnBorrowRecordRequest
{
    public List<ReturnBorrowRecordDetailRequest> BorrowRecordDetails { get; set; } = new();
    public List<LostBorrowRecordRequest> LostBorrowRecordDetails { get; set; } = new();
    public bool IsConfirmMissing { get; set; } = false;
}