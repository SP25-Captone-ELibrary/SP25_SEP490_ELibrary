using System.Text.Json.Serialization;
using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Domain.Entities;

public class ReservationQueue
{
    // Key
    public int QueueId { get; set; }
    
    // For which library item
    public int LibraryItemId { get; set; }
    
    // Item instance assigned after other people return item
    public int? LibraryItemInstanceId { get; set; } 

    // For specific user
    public Guid LibraryCardId { get; set; }
    
    // Queue status
    public ReservationQueueStatus QueueStatus { get; set; } 
    
    // Belongs to specific request (if any)
    public int? BorrowRequestId { get; set; }
    
    // Mark as reserved after requested failed
    public bool IsReservedAfterRequestFailed { get; set; }
    
    // Forecasting available datetime
    public DateTime? ExpectedAvailableDateMin { get; set; } // Best case scenario
    public DateTime? ExpectedAvailableDateMax { get; set; } // Worst case scenario
    
    // Reservation date
    public DateTime ReservationDate { get; set; }
    
    // Deadline for pickup
    public DateTime? ExpiryDate { get; set; }
    
    // Collected date
    public DateTime? CollectedDate { get; set; }
    
    // If the user was notified
    public bool IsNotified { get; set; }
    
    // Cancellation details
    public string? CancelledBy { get; set; }
    public string? CancellationReason { get; set; } 
    
    // Mapping entities
    public LibraryItem LibraryItem { get; set; } = null!;
    
    [JsonIgnore] 
    public LibraryItemInstance? LibraryItemInstance { get; set; }

    [JsonIgnore]
    public LibraryCard LibraryCard { get; set; } = null!;
    
    [JsonIgnore] 
    public BorrowRequest? BorrowRequest { get; set; }
}
