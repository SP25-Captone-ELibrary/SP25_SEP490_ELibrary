using System.Text.Json.Serialization;

namespace FPTU_ELibrary.Domain.Entities;

public class LibraryItemCondition
{
    public int ConditionId { get; set; }
    public string EnglishName { get; set; } = null!;

    public string VietnameseName { get; set; } = null!;
    
    // Mapping entities
    [JsonIgnore]
    public ICollection<LibraryItemConditionHistory> LibraryItemConditionHistories { get; set; } = new List<LibraryItemConditionHistory>();
    
    [JsonIgnore]
    public ICollection<BorrowRecordDetail> BorrowRecordDetails { get; set; } = new List<BorrowRecordDetail>();
    
    [JsonIgnore]
    public ICollection<BorrowRecordDetail> BorrowRecordDetailsReturn { get; set; } = new List<BorrowRecordDetail>();
    
    [JsonIgnore]
    public ICollection<WarehouseTrackingDetail> WarehouseTrackingDetails { get; set; } = new List<WarehouseTrackingDetail>();
}