using System.Text.Json.Serialization;

namespace FPTU_ELibrary.Domain.Entities;

public class BookEditionCopy
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
    public DateTime CreateDate { get; set; }
    public DateTime? UpdateDate { get; set; }

    // Mark as delete
    public bool IsDeleted { get; set; }

    // Mapping entities
    public LibraryShelf? Shelf { get; set; }

    [JsonIgnore]
    public BookEdition BookEdition { get; set; } = null!;

    [JsonIgnore]
    public ICollection<BorrowRecord> BorrowRecords { get; set; } = new List<BorrowRecord>();

    [JsonIgnore]
    public ICollection<BorrowRequest> BorrowRequests { get; set; } = new List<BorrowRequest>();

    public ICollection<CopyConditionHistory> CopyConditionHistories { get; set; } = new List<CopyConditionHistory>();

}
