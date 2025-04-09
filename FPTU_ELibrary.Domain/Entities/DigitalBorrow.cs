using System.Text.Json.Serialization;
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
    public string? S3WatermarkedName { get; set; }
    public BorrowDigitalStatus Status { get; set; }
    
    // References
    [JsonIgnore]
    public User User { get; set; } = null!;
    public LibraryResource LibraryResource { get; set; } = null!;
    
    // Navigations
    public ICollection<DigitalBorrowExtensionHistory> DigitalBorrowExtensionHistories { get; set; } = new List<DigitalBorrowExtensionHistory>();
}