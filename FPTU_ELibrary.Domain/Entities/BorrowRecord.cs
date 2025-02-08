using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Domain.Entities;

public class BorrowRecord
{
    // Key
    public int BorrowRecordId { get; set; }

    // Optional link to BorrowRequest (only for remote borrowing)
    public int? BorrowRequestId { get; set; }  
    
    // Foreign keys
    public Guid LibraryCardId { get; set; }
    
    // Borrow record tracking
    public DateTime BorrowDate { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? ReturnDate { get; set; }
    public BorrowRecordStatus Status { get; set; } 
    
    // True if borrowed via kiosk
    public bool SelfServiceBorrow { get; set; } 

    // Total extension time  
    public int TotalExtension { get; set; }

    // Borrow items condition tracking
    public string BorrowCondition { get; set; } = null!;
    public string? ReturnCondition { get; set; }
    public DateTime? ConditionCheckDate { get; set; }

    // Borrow record processed by which employee
    public Guid ProcessedBy { get; set; }

    // Mapping entities
    public BorrowRequest? BorrowRequest { get; set; }
    public Employee ProcessedByNavigation { get; set; } = null!;
    public LibraryCard LibraryCard { get; set; } = null!;
    
    public ICollection<BorrowRecordDetail> BorrowRecordDetails { get; set; } = new List<BorrowRecordDetail>();
    public ICollection<Fine> Fines { get; set; } = new List<Fine>();
}
