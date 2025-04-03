using System.Text.Json.Serialization;
using FPTU_ELibrary.Application.Dtos.LibraryCard;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Application.Dtos.Borrows;

public class ReservationQueueDto
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
    
    // Reservation code (only process when s.o return their item and assigned to s.o in reservation queues)
    public string? ReservationCode { get; set; }
    
    // Mark whether assigned reservation code or not 
    public bool IsAppliedLabel { get; set; }
    
    // Collected date
    public DateTime? CollectedDate { get; set; }
    
    // Assigned date
    public DateTime? AssignedDate { get; set; }
    
    // Extend pick up time
    public int TotalExtendPickup { get; set; }
    
    // If the user was notified
    public bool IsNotified { get; set; }
    
    // Cancellation details
    public string? CancelledBy { get; set; } 
    public string? CancellationReason { get; set; } 
    
    // Mapping entities
    public LibraryItemDto LibraryItem { get; set; } = null!;
    
    public LibraryItemInstanceDto? LibraryItemInstance { get; set; }

    public LibraryCardDto LibraryCard { get; set; } = null!;
    
    [JsonIgnore] 
    public BorrowRequestDto? BorrowRequest { get; set; }
}