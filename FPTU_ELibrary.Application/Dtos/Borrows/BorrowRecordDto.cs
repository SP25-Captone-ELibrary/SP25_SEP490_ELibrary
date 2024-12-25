using FPTU_ELibrary.Application.Dtos.BookEditions;
using FPTU_ELibrary.Application.Dtos.Books;
using FPTU_ELibrary.Application.Dtos.Employees;

namespace FPTU_ELibrary.Application.Dtos.Borrows;

public class BorrowRecordDto
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
    public BookEditionCopyDto? BookEditionCopy { get; set; }
    public EmployeeDto ProcessedByNavigation { get; set; } = null!;
    public UserDto Borrower { get; set; } = null!;
    public LearningMaterialDto? LearningMaterial { get; set; }
    // public ICollection<FineDto> Fines { get; set; } = new List<FineDto>();
    
    //public BorrowRequest BorrowRequest { get; set; } = null!;
}