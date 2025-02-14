using FPTU_ELibrary.Application.Dtos.Borrows;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Application.Dtos.LibraryCard;

public class LibraryCardHolderBorrowRequestDto
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
    
    public List<LibraryItemDetailDto> LibraryItems { get; set; } = new ();
}

public static class LibraryCardBorrowRequestDtoExtensions
{
    public static LibraryCardHolderBorrowRequestDto ToLibraryCardBorrowRequestDto(this BorrowRequestDto dto)
    {
        return new()
        {
            BorrowRequestId = dto.BorrowRequestId,
            LibraryCardId = dto.LibraryCardId,
            RequestDate = dto.RequestDate,
            ExpirationDate = dto.ExpirationDate,
            Status = dto.Status,
            Description = dto.Description,
            CancelledAt = dto.CancelledAt,
            CancellationReason = dto.CancellationReason,
            IsReminderSent = dto.IsReminderSent,
            LibraryItems = dto.BorrowRequestDetails
                .Select(brd => brd.LibraryItem)
                .Select(li => li.ToLibraryItemDetailDto()).ToList()
        };
    }
}