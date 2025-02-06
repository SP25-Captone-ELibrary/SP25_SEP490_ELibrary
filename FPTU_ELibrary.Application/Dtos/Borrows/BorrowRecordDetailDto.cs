using FPTU_ELibrary.Application.Dtos.LibraryItems;

namespace FPTU_ELibrary.Application.Dtos.Borrows;

public class BorrowRecordDetailDto
{
    public int BorrowRecordDetailId { get; set; }
    
    // Specific borrow record
    public int BorrowRecordId { get; set; }
    
    // Specific library item instance
    public int LibraryItemInstanceId { get; set; }
    
    // Mapping entities
    public BorrowRecordDto BorrowRecord { get; set; } = null!;
    public LibraryItemInstanceDto LibraryItemInstance { get; set; } = null!;
}