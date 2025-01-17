using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Domain.Entities;

public class DigitalBorrow
{
    public int DigitalBorrowId { get; set; }
    public int ResourceId { get; set; }
    public Guid UserId { get; set; }
    public DateTime RegisterDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public bool IsExtended { get; set; }
    public int ExtensionCount { get; set; }
    public BorrowDigitalStatus Status { get; set; }
    
    public LibraryResource LibraryResource { get; set; } = null!;
    public User User { get; set; } = null!;
    
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}