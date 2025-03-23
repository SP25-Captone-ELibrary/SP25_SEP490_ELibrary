using System.Text.Json.Serialization;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Application.Dtos.Payments;

namespace FPTU_ELibrary.Application.Dtos.Borrows;

public class BorrowRequestResourceDto
{
    // Key
    public int BorrowRequestResourceId { get; set; }
    
    // For which borrow request
    public int BorrowRequestId { get; set; }
 
    // For which resource
    public int ResourceId { get; set; }

    // Request resource details
    public string ResourceTitle { get; set; } = null!;
    public decimal BorrowPrice { get; set; }
    public int DefaultBorrowDurationDays { get; set; }
    
    // Transaction information
    public int? TransactionId { get; set; }
    
    // References
    [JsonIgnore] 
    public BorrowRequestDto BorrowRequest { get; set; } = null!;

    public LibraryResourceDto LibraryResource { get; set; } = null!;
    
    public TransactionDto? Transaction { get; set; } = null!;
}