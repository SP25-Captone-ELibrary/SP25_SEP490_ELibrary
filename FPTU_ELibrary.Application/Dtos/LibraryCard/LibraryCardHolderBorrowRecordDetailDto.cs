using FPTU_ELibrary.Application.Dtos.Borrows;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Application.Dtos.LibraryCard;

public class LibraryCardHolderBorrowRecordDetailDto
{
    public int BorrowRecordDetailId { get; set; }
    
    // Specific borrow record
    public int BorrowRecordId { get; set; }

    // Borrow record details
    public int ConditionId { get; set; } 
    public int? ReturnConditionId { get; set; }
    public DateTime? ConditionCheckDate { get; set; }
    public DateTime? ReturnDate { get; set; }
    public DateTime DueDate { get; set; }
    public BorrowRecordStatus Status { get; set; } 
    
    // Remind user before expiration (via email or system notification)
    public bool IsReminderSent { get; set; }
    
    // Total extension time  
    public int TotalExtension { get; set; }
    
    // References
    public LibraryItemDto LibraryItem { get; set; } = null!;
    public LibraryItemConditionDto Condition { get; set; } = null!;
    public LibraryItemConditionDto? ReturnCondition { get; set; } = null!;
    
    // Navigations
    public List<string> ConditionImages { get; set; } = new();
    public List<BorrowDetailExtensionHistoryDto> BorrowDetailExtensionHistories { get; set; } = new();
}