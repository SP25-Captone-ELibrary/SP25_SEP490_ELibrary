using System.Text.Json.Serialization;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Application.Dtos.Borrows;

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
    
    // References
    [JsonIgnore]
    public LibraryResourceDto LibraryResource { get; set; } = null!;
    public UserDto User { get; set; } = null!;
    
    // Navigations
    public ICollection<DigitalBorrowExtensionHistoryDto> DigitalBorrowExtensionHistories { get; set; } = new List<DigitalBorrowExtensionHistoryDto>();
}