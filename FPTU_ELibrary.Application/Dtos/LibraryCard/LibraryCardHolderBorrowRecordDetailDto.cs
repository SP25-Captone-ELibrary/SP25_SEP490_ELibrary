using FPTU_ELibrary.Application.Dtos.LibraryItems;

namespace FPTU_ELibrary.Application.Dtos.LibraryCard;

public class LibraryCardHolderBorrowRecordDetailDto
{
    public int BorrowRecordDetailId { get; set; }
    
    // Specific borrow record
    public int BorrowRecordId { get; set; }
    
    public LibraryItemDto LibraryItem { get; set; } = null!;
    
    public int ConditionId { get; set; } 
    public int? ReturnConditionId { get; set; }
    public DateTime? ConditionCheckDate { get; set; }
    public LibraryItemConditionDto Condition { get; set; } = null!;
    public LibraryItemConditionDto? ReturnCondition { get; set; } = null!;
    public List<string> ConditionImages { get; set; } = new();
}