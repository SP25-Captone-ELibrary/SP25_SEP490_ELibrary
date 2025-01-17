using System.Text.Json.Serialization;
using FPTU_ELibrary.Application.Dtos.LibraryItems;

namespace FPTU_ELibrary.Application.Dtos.Borrows;

public class BorrowRequestDto
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
    public LibraryItemDto? LibraryItem { get; set; }
    public LibraryItemInstanceDto? LibraryItemInstance { get; set; }
    public UserDto User { get; set; } = null!;

    [JsonIgnore]
    public ICollection<BorrowRecordDto> BorrowRecords { get; set; } = new List<BorrowRecordDto>();
}