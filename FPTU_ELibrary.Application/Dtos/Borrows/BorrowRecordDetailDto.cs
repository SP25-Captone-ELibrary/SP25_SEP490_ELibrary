using System.Text.Json.Serialization;
using FPTU_ELibrary.Application.Dtos.LibraryItems;

namespace FPTU_ELibrary.Application.Dtos.Borrows;

public class BorrowRecordDetailDto
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
    public BorrowRecordDto BorrowRecord { get; set; } = null!;
    public LibraryItemInstanceDto LibraryItemInstance { get; set; } = null!;
    public LibraryItemConditionDto Condition { get; set; } = null!;
}