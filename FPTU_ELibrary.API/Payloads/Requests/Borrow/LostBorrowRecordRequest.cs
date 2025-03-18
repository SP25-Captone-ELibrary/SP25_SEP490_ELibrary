using FPTU_ELibrary.API.Payloads.Requests.Fine;

namespace FPTU_ELibrary.API.Payloads.Requests.Borrow;

public class LostBorrowRecordRequest
{
    public int BorrowRecordDetailId { get; set; }
    public List<CreateFineRequest> Fines { get; set; } = new();
}