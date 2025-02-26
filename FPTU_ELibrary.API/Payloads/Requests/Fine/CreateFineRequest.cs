namespace FPTU_ELibrary.API.Payloads.Requests.Fine;

public class CreateFineRequest
{
    public int FinePolicyId { get; set; } 
    public int BorrowRecordId { get; set; }
}