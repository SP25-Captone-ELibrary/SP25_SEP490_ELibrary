using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Domain.Entities;

public class Transaction
{
    public int TransactionId { get; set; }
    public string? TransactionCode { get; set; }
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; } 
    public TransactionStatus TransactionStatus { get; set; }
    public TransactionType TransactionType { get; set; }
    public DateTime? TransactionDate { get; set; }
    public DateTime ExpiredAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    public int? FineId { get; set; }
    public int? ResourceId { get; set; }
    public int? LibraryCardPackageId { get; set; }
    // Transaction method
    public TransactionMethod? TransactionMethod { get; set; }
    public int? PaymentMethodId { get; set; }
    
    // Store payment information
    public string? PaymentUrl { get; set; }
    
    // Mapping entities
    public User User { get; set; } = null!;
    public Fine? Fine { get; set; }
    public LibraryResource? LibraryResource { get; set; }
    public LibraryCardPackage? LibraryCardPackage { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
}