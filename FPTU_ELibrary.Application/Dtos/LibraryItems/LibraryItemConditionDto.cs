using System.Text.Json.Serialization;
using FPTU_ELibrary.Application.Dtos.Borrows;
using FPTU_ELibrary.Application.Dtos.WarehouseTrackings;

namespace FPTU_ELibrary.Application.Dtos.LibraryItems;

public class LibraryItemConditionDto
{
    public int ConditionId { get; set; }
    public string EnglishName { get; set; } = null!;

    public string VietnameseName { get; set; } = null!;
    
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Mapping entities
    [JsonIgnore]
    public ICollection<LibraryItemConditionHistoryDto> LibraryItemConditionHistories { get; set; } = new List<LibraryItemConditionHistoryDto>();
    
    [JsonIgnore]
    public ICollection<BorrowRecordDetailDto> BorrowRecordDetails { get; set; } = new List<BorrowRecordDetailDto>();
    
    [JsonIgnore]
    public ICollection<BorrowRecordDetailDto> BorrowRecordDetailsReturn { get; set; } = new List<BorrowRecordDetailDto>();
    
    [JsonIgnore]
    public ICollection<WarehouseTrackingDetailDto> WarehouseTrackingDetails { get; set; } = new List<WarehouseTrackingDetailDto>();
}