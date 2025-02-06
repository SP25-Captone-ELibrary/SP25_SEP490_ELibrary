using System.Text.Json.Serialization;
using FPTU_ELibrary.Application.Dtos.LibraryItems;

namespace FPTU_ELibrary.Application.Dtos.Borrows;

public class BorrowRequestDetailDto
{
    public int BorrowRequestDetailId { get; set; }

    // Request details
    public int BorrowRequestId { get; set; }
    
    // Request for which item
    public int LibraryItemId { get; set; }

    // Mapping entities
    [JsonIgnore]
    public BorrowRequestDto BorrowRequest { get; set; } = null!;
    
    public LibraryItemDto LibraryItem { get; set; } = null!;
}