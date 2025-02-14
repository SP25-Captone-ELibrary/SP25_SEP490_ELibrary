using FPTU_ELibrary.Application.Dtos.Borrows;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Application.Dtos.LibraryCard;

public class LibraryCardHolderReservationQueueDto
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
    
    // Forecasting available datetime
    public DateTime? ExpectedAvailableDateMin { get; set; } // Best case scenario
    public DateTime? ExpectedAvailableDateMax { get; set; } // Worst case scenario
    
    // Reservation date
    public DateTime ReservationDate { get; set; }
    
    // Deadline for pickup
    public DateTime? ExpiryDate { get; set; }
    
    // If the user was notified
    public bool IsNotified { get; set; }
    
    // Cancellation details
    public string? CancelledBy { get; set; }
    public string? CancellationReason { get; set; } 
    
    // Mapping entities
    public LibraryItemDto LibraryItem { get; set; } = null!;
    
    public LibraryItemInstanceDto? LibraryItemInstance { get; set; }
}

public static class LibraryCardHolderReservationQueueDtoExtensions
{
    public static LibraryCardHolderReservationQueueDto ToCardHolderReservationQueueDto(this ReservationQueueDto dto)
    {
        return new()
        {
            QueueId = dto.QueueId,
            LibraryItemId = dto.LibraryItemId,
            LibraryItemInstanceId = dto.LibraryItemInstanceId,
            LibraryCardId = dto.LibraryCardId,
            QueueStatus = dto.QueueStatus,
            ExpectedAvailableDateMin = dto.ExpectedAvailableDateMin,
            ExpectedAvailableDateMax = dto.ExpectedAvailableDateMax,
            ReservationDate = dto.ReservationDate,
            ExpiryDate = dto.ExpiryDate,
            IsNotified = dto.IsNotified,
            CancelledBy = dto.CancelledBy,
            CancellationReason = dto.CancellationReason,
            LibraryItem = dto.LibraryItem,
            LibraryItemInstance = dto.LibraryItemInstance
        };
    }
}