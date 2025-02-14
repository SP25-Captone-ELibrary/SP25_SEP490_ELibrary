using System.Text.Json.Serialization;

namespace FPTU_ELibrary.Domain.Entities;

public class BorrowRecordDetail
{
    public int BorrowRecordDetailId { get; set; }
    
    // Specific borrow record
    public int BorrowRecordId { get; set; }
    
    // Specific library item instance
    public int LibraryItemInstanceId { get; set; }

    // Maximum of 5 image public id (use only when using Kiosk machine)
    public string? ImagePublicIds { get; set; }
    
    // Borrow items condition tracking
    public int ConditionId { get; set; }
    public int? ReturnConditionId { get; set; }
    public DateTime? ConditionCheckDate { get; set; }
    
    // Mapping entities
    [JsonIgnore]
    public BorrowRecord BorrowRecord { get; set; } = null!;
    
    public LibraryItemInstance LibraryItemInstance { get; set; } = null!;
    public LibraryItemCondition Condition { get; set; } = null!;
}