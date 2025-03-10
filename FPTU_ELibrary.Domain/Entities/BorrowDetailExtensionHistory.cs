using System.Text.Json.Serialization;

namespace FPTU_ELibrary.Domain.Entities;

public class BorrowDetailExtensionHistory
{
    // Key
    public int BorrowDetailExtensionHistoryId { get; set; }
    
    // For which borrow record detail
    public int BorrowRecordDetailId { get; set; }
    
    // Extension details
    public DateTime ExtensionDate { get; set; }
    public DateTime NewExpiryDate { get; set; }
    public int ExtensionNumber { get; set; }
    
    // References
    [JsonIgnore] 
    public BorrowRecordDetail BorrowRecordDetail { get; set; } = null!;
}