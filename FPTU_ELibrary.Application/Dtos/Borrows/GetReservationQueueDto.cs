using System.Text.Json.Serialization;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Application.Dtos.Borrows;

public class GetReservationQueueDto
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
    
    // Mark whether is assignable
    public bool IsAssignable { get; set; }
    
    // Mapping entities
    public LibraryItemDetailDto LibraryItem { get; set; } = null!;
    
    public LibraryItemInstanceDto? LibraryItemInstance { get; set; }

    [JsonIgnore] 
    public GetBorrowRequestDto? BorrowRequest { get; set; }
}

public static class GetReservationQueueDtoExtensions
{
    public static GetReservationQueueDto ToGetReservationQueueDto(this ReservationQueueDto dto,
        bool? isAssignable = null)
    {
        return new()
        {
            QueueId = dto.QueueId,
            LibraryItemId = dto.LibraryItemId,
            LibraryItemInstanceId = dto.LibraryItemInstanceId,
            LibraryCardId = dto.LibraryCardId,
            QueueStatus = dto.QueueStatus,
            BorrowRequestId = dto.BorrowRequestId,
            IsReservedAfterRequestFailed = dto.IsReservedAfterRequestFailed,
            ExpectedAvailableDateMin = dto.ExpectedAvailableDateMin,
            ExpectedAvailableDateMax = dto.ExpectedAvailableDateMax,
            ReservationDate = dto.ReservationDate,
            ReservationCode = dto.ReservationCode,
            IsAppliedLabel = dto.IsAppliedLabel,
            ExpiryDate = dto.ExpiryDate,
            CollectedDate = dto.CollectedDate,
            AssignedDate = dto.AssignedDate,
            TotalExtendPickup = dto.TotalExtendPickup,
            IsNotified = dto.IsNotified,
            CancelledBy = dto.CancelledBy,
            CancellationReason = dto.CancellationReason,
            BorrowRequest = dto.BorrowRequest?.ToGetBorrowRequestDto(),
            LibraryItem = dto.LibraryItem != null! ? dto.LibraryItem.ToLibraryItemDetailDto() : null!,
            LibraryItemInstance = dto.LibraryItemInstance,
            IsAssignable = isAssignable ?? false
        };
    }
}