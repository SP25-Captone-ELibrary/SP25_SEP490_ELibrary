using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Domain.Entities;

public class Transaction
{
    public int TransactionId { get; set; }
    public string TransactionCode { get; set; } = null!;
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = null!;
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

    public int InvoiceId { get; set; }
    
    // Mapping entities
    public User User { get; set; } = null!;
    public PaymentMethod PaymentMethod { get; set; } = null!;
    public Fine? Fine { get; set; }
    public DigitalBorrow? DigitalBorrow { get; set; }
    public LibraryCardPackage? LibraryCardPackage { get; set; }
    public Invoice Invoice { get; set; } = null!;
}