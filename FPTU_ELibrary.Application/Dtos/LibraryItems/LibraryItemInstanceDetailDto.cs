using FPTU_ELibrary.Application.Dtos.Borrows;

namespace FPTU_ELibrary.Application.Dtos.LibraryItems;

public class LibraryItemInstanceDetailDto
{
    // Key
    public int LibraryItemInstanceId { get; set; }

    // Copy of which item
    public int LibraryItemId { get; set; }
    
    // Copy code and its status
    public string Barcode { get; set; } = null!;
    public string Status { get; set; } = null!;
    
    // Creation and update datetime
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = null!;
    public string? UpdatedBy { get; set; }

    // Mark as delete
    public bool IsDeleted { get; set; }

    // Mapping entities
    public LibraryItemDto LibraryItem { get; set; } = null!;

    public List<BorrowRecordDetailDto> BorrowRecordDetails { get; set; } = new();

    public List<BorrowRequestDetailDto> BorrowRequestDetails { get; set; } = new();
    
    public List<ReservationQueueDto> ReservationQueues { get; set; } = new();
    
    public List<LibraryItemConditionHistoryDto> LibraryItemConditionHistories { get; set; } = new();
}

public static class LibraryItemInstanceDetailDtoExtensions
{
    public static LibraryItemInstanceDetailDto ToItemInstanceDetailDtoAsync(this LibraryItemInstanceDto dto)
    {
        return new()
        {
            LibraryItemInstanceId = dto.LibraryItemInstanceId,
            LibraryItemId = dto.LibraryItemId,
            Barcode = dto.Barcode,
            Status = dto.Status,
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt,
            CreatedBy = dto.CreatedBy,
            UpdatedBy = dto.UpdatedBy,
            IsDeleted = dto.IsDeleted,
            LibraryItem = dto.LibraryItem,
            BorrowRecordDetails = dto.BorrowRecordDetails.Any() ? dto.BorrowRecordDetails.ToList() : new(),
            BorrowRequestDetails = dto.BorrowRequestDetails.Any() ? dto.BorrowRequestDetails.ToList() : new(),
            ReservationQueues = dto.ReservationQueues.Any() ? dto.ReservationQueues.ToList() : new(),
            LibraryItemConditionHistories = dto.LibraryItemConditionHistories.Any() 
                ? dto.LibraryItemConditionHistories.ToList() : new(),
        };
    }
}