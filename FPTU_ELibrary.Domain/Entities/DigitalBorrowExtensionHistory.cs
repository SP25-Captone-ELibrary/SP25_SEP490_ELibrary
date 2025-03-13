using System.Text.Json.Serialization;

namespace FPTU_ELibrary.Domain.Entities;

public class DigitalBorrowExtensionHistory
{
    // Key
    public int DigitalExtensionHistoryId { get; set; }
    
    // For which digital borrow
    public int DigitalBorrowId { get; set; }
    
    // Extension details
    public DateTime ExtensionDate { get; set; }
    public DateTime NewExpiryDate { get; set; }
    public decimal ExtensionFee { get; set; }
    public int ExtensionNumber { get; set; }
    
    // References
    [JsonIgnore] 
    public DigitalBorrow DigitalBorrow { get; set; } = null!;
}