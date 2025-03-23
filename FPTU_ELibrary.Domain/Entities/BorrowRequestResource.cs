using System.Text.Json.Serialization;

namespace FPTU_ELibrary.Domain.Entities;

public class BorrowRequestResource
{
    // Key
    public int BorrowRequestResourceId { get; set; }
    
    // For which borrow request
    public int BorrowRequestId { get; set; }
 
    // For which resource
    public int ResourceId { get; set; }

    // Request resource details
    public string ResourceTitle { get; set; } = null!;
    public decimal BorrowPrice { get; set; }
    public int DefaultBorrowDurationDays { get; set; }
    
    // Transaction information
    public int? TransactionId { get; set; }
    
    // References
    [JsonIgnore] 
    public BorrowRequest BorrowRequest { get; set; } = null!;

    public LibraryResource LibraryResource { get; set; } = null!;

    public Transaction? Transaction { get; set; } = null!;
}