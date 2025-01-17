using System.Text.Json.Serialization;

namespace FPTU_ELibrary.Domain.Entities;

public class BorrowRequest
{
    // Key
    public int BorrowRequestId { get; set; }

    // Request for which item
    public int? LibraryItemId { get; set; }

    // Request for particular instance 
    public int? LibraryItemInstanceId { get; set; }

    // Who make request
    public Guid UserId { get; set; }

    // Create and expiration datetime
    public DateTime RequestDate { get; set; }
    public DateTime ExpirationDate { get; set; }

    // Request detail and status
    public string Status { get; set; } = null!;
    public string BorrowType { get; set; } = null!;
    public string? Description { get; set; }

    // Mapping entities
    public LibraryItem? LibraryItem { get; set; }
    public LibraryItemInstance? LibraryItemInstance { get; set; }
    public User User { get; set; } = null!;

    [JsonIgnore]
    public ICollection<BorrowRecord> BorrowRecords { get; set; } = new List<BorrowRecord>();
}
