using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Domain.Entities;

public class BorrowRequest
{
    // Key
    public int BorrowRequestId { get; set; }

    // Who make request
    public Guid LibraryCardId { get; set; }

    // Create and expiration datetime
    public DateTime RequestDate { get; set; }
    public DateTime ExpirationDate { get; set; }

    // Request detail and status
    public BorrowRequestStatus Status { get; set; } 
    public string? Description { get; set; }
    
    // Cancellation properties
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    
    // Remind user before expiration (via email or system notification)
    public bool IsReminderSent { get; set; }

    // Count total request item
    public int TotalRequestItem { get; set; }
    
    // Mapping entities
    public LibraryCard LibraryCard { get; set; } = null!;
    
    public ICollection<BorrowRequestDetail> BorrowRequestDetails { get; set; } = new List<BorrowRequestDetail>();

    public ICollection<ReservationQueue> ReservationQueues { get; set; } = new List<ReservationQueue>();
}
