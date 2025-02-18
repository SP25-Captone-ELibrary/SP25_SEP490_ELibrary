using FPTU_ELibrary.Application.Dtos.Employees;
using FPTU_ELibrary.Application.Dtos.Fine;
using FPTU_ELibrary.Application.Dtos.LibraryCard;
using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Application.Dtos.Borrows;

public class BorrowRecordDto
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
    
    // Borrow type
    public BorrowType BorrowType { get; set; }
    
    // True if borrowed via kiosk
    public bool SelfServiceBorrow { get; set; } 
    
    // True if return via kiosk 
    public bool? SelfServiceReturn { get; set; }
    
    // Total extension time  
    public int TotalExtension { get; set; }
    
    // Total record item
    public int TotalRecordItem { get; set; }
    
    // Borrow record processed by which employee
    public Guid? ProcessedBy { get; set; }

    // Mapping entities
    public BorrowRequestDto? BorrowRequest { get; set; }
    public EmployeeDto? ProcessedByNavigation { get; set; } = null!;
    public LibraryCardDto LibraryCard { get; set; } = null!;
    
    public ICollection<BorrowRecordDetailDto> BorrowRecordDetails { get; set; } = new List<BorrowRecordDetailDto>();
    public ICollection<FineDto> Fines { get; set; } = new List<FineDto>();
}