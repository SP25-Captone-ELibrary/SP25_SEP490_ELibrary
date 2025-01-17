using System.Text.Json.Serialization;
using FPTU_ELibrary.Domain.Interfaces;

namespace FPTU_ELibrary.Domain.Entities;

public class LibraryItemInstance : IAuditableEntity
{
    // Key
    public int LibraryItemInstanceId { get; set; }

    // Copy of which item
    public int LibraryItemId { get; set; }
    
    // Copy code and its status
    public string Barcode { get; set; } = null!;
    public string Status { get; set; } = null!;
    
    // Creation and update datetime
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = null!;
    public string? UpdatedBy { get; set; }

    // Mark as delete
    public bool IsDeleted { get; set; }

    // Mapping entities
    [JsonIgnore]
    public LibraryItem LibraryItem { get; set; } = null!;

    [JsonIgnore]
    public ICollection<BorrowRecord> BorrowRecords { get; set; } = new List<BorrowRecord>();

    [JsonIgnore]
    public ICollection<BorrowRequest> BorrowRequests { get; set; } = new List<BorrowRequest>();
    
    public ICollection<LibraryItemConditionHistory> LibraryItemConditionHistories { get; set; } = new List<LibraryItemConditionHistory>();
}
