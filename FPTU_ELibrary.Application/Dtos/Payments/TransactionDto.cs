using System.Text.Json.Serialization;
using FPTU_ELibrary.Application.Dtos.Borrows;
using FPTU_ELibrary.Application.Dtos.Fine;
using FPTU_ELibrary.Application.Dtos.LibraryCard;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Application.Dtos.Payments;

public class TransactionDto
{
    public int TransactionId { get; set; }
    public string? TransactionCode { get; set; }
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public TransactionStatus TransactionStatus { get; set; }
    public TransactionType TransactionType { get; set; }
    public DateTime? TransactionDate { get; set; }
    public DateTime? ExpiredAt { get; set; }
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
    public string? QrCode { get; set; }
    
    // Mapping entities
    public UserDto User { get; set; } = null!;
    public FineDto? Fine { get; set; }
    public LibraryResourceDto? LibraryResource { get; set; }
    public LibraryCardPackageDto? LibraryCardPackage { get; set; }
    public PaymentMethodDto? PaymentMethod { get; set; }
    
    // Navigations
    [JsonIgnore]
    public ICollection<BorrowRequestResourceDto> BorrowRequestResources { get; set; } = new List<BorrowRequestResourceDto>();
}