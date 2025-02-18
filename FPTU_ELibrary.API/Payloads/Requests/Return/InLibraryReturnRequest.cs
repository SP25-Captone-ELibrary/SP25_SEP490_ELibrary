namespace FPTU_ELibrary.API.Payloads.Requests.Return;

public class InLibraryReturnRequest
{
    // For which record
    public int BorrowRecordId { get; set; }
    
    // Library card
    public Guid LibraryCardId { get; set; }
    
    // 
}