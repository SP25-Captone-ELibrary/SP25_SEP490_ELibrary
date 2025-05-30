using System.Text.Json.Serialization;
using FPTU_ELibrary.Application.Dtos.LibraryCard;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Application.Dtos.Borrows;

public class BorrowRequestDto
{
    // Key
    public int BorrowRequestId { get; set; }

    // Who make request
    public Guid LibraryCardId { get; set; }

    // Create and expiration datetime
    public DateTime RequestDate { get; set; }
    public DateTime? ExpirationDate { get; set; }

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
    public LibraryCardDto LibraryCard { get; set; } = null!;

    public ICollection<BorrowRequestDetailDto> BorrowRequestDetails { get; set; } = new List<BorrowRequestDetailDto>();
    
    public ICollection<ReservationQueueDto> ReservationQueues { get; set; } = new List<ReservationQueueDto>();
    
    public ICollection<BorrowRequestResourceDto> BorrowRequestResources { get; set; } = new List<BorrowRequestResourceDto>();
}