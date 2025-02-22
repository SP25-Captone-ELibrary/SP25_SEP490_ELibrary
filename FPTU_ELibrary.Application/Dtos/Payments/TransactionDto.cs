using FPTU_ELibrary.Application.Dtos.Fine;
using FPTU_ELibrary.Application.Dtos.LibraryCard;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Application.Dtos.Payments;

public class TransactionDto
{
    public int TransactionId { get; set; }
    public string? TransactionCode { get; set; } = null!;
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public TransactionStatus TransactionStatus { get; set; }
    public TransactionType TransactionType { get; set; }
    public DateTime? TransactionDate { get; set; } 
    public DateTime CreatedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }

    public int PaymentMethodId { get; set; }
    public int? FineId { get; set; }
    public int? DigitalBorrowId { get; set; }
    public int? LibraryCardPackageId { get; set; }

    public int? InvoiceId { get; set; }
    
    // Mapping entities
    public UserDto User { get; set; } = null!;
    public PaymentMethodDto PaymentMethod { get; set; } = null!;
    public FineDto? Fine { get; set; }
    public DigitalBorrowDto? DigitalBorrow { get; set; }
    public LibraryCardPackageDto? LibraryCardPackage { get; set; }
    public InvoiceDto? Invoice { get; set; } = null!;
}