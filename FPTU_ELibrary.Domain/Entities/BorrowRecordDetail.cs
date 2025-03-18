using System.Text.Json.Serialization;
using FPTU_ELibrary.Domain.Common.Enums;

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
    
    // Status
    public BorrowRecordStatus Status { get; set; } 
    
    // Borrow items record tracking
    public DateTime DueDate { get; set; }
    public DateTime? ReturnDate { get; set; }
    public DateTime? ConditionCheckDate { get; set; }
    
    // Remind user before expiration (via email or system notification)
    public bool IsReminderSent { get; set; }
    
    // Total extension time  
    public int TotalExtension { get; set; }
    
    // Processed return by
    public Guid? ProcessedReturnBy { get; set; }
    
    // Mapping entities
    [JsonIgnore]
    public BorrowRecord BorrowRecord { get; set; } = null!;

    public Employee? ProcessedReturnByNavigation { get; set; }
    public LibraryItemInstance LibraryItemInstance { get; set; } = null!;
    public LibraryItemCondition Condition { get; set; } = null!;
    public LibraryItemCondition? ReturnCondition { get; set; }
    
    // Navigations
    public ICollection<BorrowDetailExtensionHistory> BorrowDetailExtensionHistories { get; set; } = new List<BorrowDetailExtensionHistory>();
    public ICollection<Fine> Fines { get; set; } = new List<Fine>();
}