namespace FPTU_ELibrary.API.Payloads.Requests;

public class CreateFineWithDigitalBorrow
{
    public int FinePolicyId { get; set; }
    public int BorrowRecordId { get; set; }
}