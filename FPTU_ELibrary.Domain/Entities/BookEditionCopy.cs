using System.Text.Json.Serialization;
using FPTU_ELibrary.Domain.Interfaces;

namespace FPTU_ELibrary.Domain.Entities;

public class BookEditionCopy : IAuditableEntity
{
    // Key
    public int BookEditionCopyId { get; set; }

    // Copy of which edition
    public int BookEditionId { get; set; }
    
    // Locate in which shelf
    public int? ShelfId { get; set; }

    // Copy code and its status
    public string? Code { get; set; }
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
    public BookEdition BookEdition { get; set; } = null!;

    [JsonIgnore]
    public ICollection<BorrowRecord> BorrowRecords { get; set; } = new List<BorrowRecord>();

    [JsonIgnore]
    public ICollection<BorrowRequest> BorrowRequests { get; set; } = new List<BorrowRequest>();

    public ICollection<CopyConditionHistory> CopyConditionHistories { get; set; } = new List<CopyConditionHistory>();
}
