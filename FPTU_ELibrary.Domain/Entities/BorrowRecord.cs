using System.Text.Json.Serialization;

namespace FPTU_ELibrary.Domain.Entities;

public class BorrowRecord
{
    // Key
    public int BorrowRecordId { get; set; }

    // Foreign keys
    public int? BookEditionCopyId { get; set; }
    public int? LearningMaterialId { get; set; }
    public Guid BorrowerId { get; set; }
    //public int BorrowRequestId { get; set; }


    // Borrow record tracking
    public DateTime BorrowDate { get; set; }
    public string BorrowType { get; set; } = null!;
    public DateTime DueDate { get; set; }
    public DateTime? ReturnDate { get; set; }
    public string Status { get; set; } = null!;

    // Total time allow to extend borrow days
    public int ExtensionLimit { get; set; }

    
    // Borrow items condition tracking
    public string BorrowCondition { get; set; } = null!;
    public string? ReturnCondition { get; set; }
    public DateTime? ConditionCheckDate { get; set; }


    // Borrow Request information
    public DateTime RequestDate { get; set; }
    public DateTime ProcessedDate { get; set; }
    public Guid ProcessedBy { get; set; }
    public decimal? DepositFee { get; set; }

    // Refund information
	public bool? DepositRefunded { get; set; }
	public DateTime? RefundDate { get; set; }


    // Mapping entities
	public BookEditionCopy? BookEditionCopy { get; set; }
    public Employee ProcessedByNavigation { get; set; } = null!;
    public User Borrower { get; set; } = null!;
    public LearningMaterial? LearningMaterial { get; set; }
    public ICollection<Fine> Fines { get; set; } = new List<Fine>();
    
    //public BorrowRequest BorrowRequest { get; set; } = null!;
}
