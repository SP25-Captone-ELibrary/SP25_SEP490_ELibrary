using FPTU_ELibrary.API.Payloads.Requests.Fine;

namespace FPTU_ELibrary.API.Payloads.Requests.Borrow;

public class ReturnBorrowRecordDetailRequest
{
    public int LibraryItemInstanceId { get; set; }
    public int ReturnConditionId { get; set; }
    public List<string>? ConditionImages = new();
    public List<CreateFineRequest> Fines { get; set; } = new();
}