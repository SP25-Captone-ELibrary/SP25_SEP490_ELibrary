namespace FPTU_ELibrary.Domain.Entities;

public class BorrowRecordDetail
{
    public int BorrowRecordDetailId { get; set; }
    
    // Specific borrow record
    public int BorrowRecordId { get; set; }
    
    // Specific library item instance
    public int LibraryItemInstanceId { get; set; }
    
    // Mapping entities
    public BorrowRecord BorrowRecord { get; set; } = null!;
    public LibraryItemInstance LibraryItemInstance { get; set; } = null!;
}