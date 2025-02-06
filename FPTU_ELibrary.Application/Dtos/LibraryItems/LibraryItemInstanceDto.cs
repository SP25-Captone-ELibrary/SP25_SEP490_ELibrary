using System.Text.Json.Serialization;
using FPTU_ELibrary.Application.Dtos.Borrows;

namespace FPTU_ELibrary.Application.Dtos.LibraryItems;

public class LibraryItemInstanceDto
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
    [JsonIgnore]
    public LibraryItemDto LibraryItem { get; set; } = null!;

    [JsonIgnore]
    public ICollection<BorrowRecordDetailDto> BorrowRecordDetails { get; set; } = new List<BorrowRecordDetailDto>();

    [JsonIgnore]
    public ICollection<BorrowRequestDetailDto> BorrowRequestDetails { get; set; } = new List<BorrowRequestDetailDto>();
    
    [JsonIgnore]
    public ICollection<ReservationQueueDto> ReservationQueues { get; set; } = new List<ReservationQueueDto>();
    
    public ICollection<LibraryItemConditionHistoryDto> LibraryItemConditionHistories { get; set; } = new List<LibraryItemConditionHistoryDto>();
}