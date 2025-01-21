using System.Text.Json.Serialization;
using FPTU_ELibrary.Application.Dtos.Payments;
using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Application.Dtos.LibraryItems;

public class DigitalBorrowDto
{
    public int DigitalBorrowId { get; set; }
    public int ResourceId { get; set; }
    public Guid UserId { get; set; }
    public DateTime RegisterDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public bool IsExtended { get; set; }
    public int ExtensionCount { get; set; }
    public BorrowDigitalStatus Status { get; set; }
    
    [JsonIgnore]
    public LibraryResourceDto LibraryResource { get; set; } = null!;
    public UserDto User { get; set; } = null!;
    
    [JsonIgnore]
    public ICollection<TransactionDto> Transactions { get; set; } = new List<TransactionDto>();
}