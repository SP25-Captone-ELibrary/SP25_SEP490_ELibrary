using System.Text.Json.Serialization;

namespace FPTU_ELibrary.Domain.Entities;

public class BorrowRequestDetail
{
    public int BorrowRequestDetailId { get; set; }

    // Request details
    public int BorrowRequestId { get; set; }
    
    // Request for which item
    public int LibraryItemId { get; set; }

    // Mapping entities
    [JsonIgnore]
    public BorrowRequest BorrowRequest { get; set; } = null!;
    
    [JsonIgnore]
    public LibraryItem LibraryItem { get; set; } = null!;
}