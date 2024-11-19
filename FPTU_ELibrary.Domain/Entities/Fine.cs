namespace FPTU_ELibrary.Domain.Entities;

public class Fine
{
    // Key
    public int FineId { get; set; }

    // Fine for which borrow record
    public int BorrowRecordId { get; set; }

    // Specific fine
    public int FinePolicyId { get; set; }
    
    // Fine detail
    public string? FineNote { get; set; }
    public decimal Amount { get; set; }
    public string PaidStatus { get; set; } = null!;
    public DateTime? PaymentDate { get; set; }
    public DateTime CreateDate { get; set; }
    public Guid CreateBy { get; set; }
    
    // Compensation handling properties
    public string? CompensationStatus { get; set; }
    public DateTime? CompensationDate { get; set; }
    public Guid? CompensateBy { get; set; }
    public string? CompensateType { get; set; }
    public string? CompensationNote { get; set; }

    // Mapping entities
    public BorrowRecord BorrowRecord { get; set; } = null!;
    public Employee CreateByNavigation { get; set; } = null!;
    public FinePolicy FinePolicy { get; set; } = null!;
}
