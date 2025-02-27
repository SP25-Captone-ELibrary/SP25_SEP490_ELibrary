using FPTU_ELibrary.Application.Dtos.Fine;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Application.Dtos.Payments;
using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Application.Dtos.LibraryCard;

public class LibraryCardHolderTransactionDto
{
    public int TransactionId { get; set; }
    public string TransactionCode { get; set; } = null!;
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
    
    // Mapping entities
    public UserDto User { get; set; } = null!;
    public PaymentMethodDto? PaymentMethod { get; set; }
    public FineDto? Fine { get; set; }
    public LibraryResourceDto? LibraryResource { get; set; }
    public LibraryCardPackageDto? LibraryCardPackage { get; set; }
}

public static class LibraryCardHolderTransactionExtensions
{
    public static LibraryCardHolderTransactionDto ToCardHolderTransactionDto(this TransactionDto dto)
    {
        return new()
        {
            TransactionId = dto.TransactionId,
            TransactionCode = dto.TransactionCode ?? string.Empty,
            UserId = dto.UserId,
            Amount = dto.Amount,
            Description = dto.Description,
            TransactionStatus = dto.TransactionStatus,
            TransactionType = dto.TransactionType,
            TransactionDate = dto.TransactionDate,
            CreatedAt = dto.CreatedAt,
            ExpiredAt = dto.ExpiredAt,
            TransactionMethod = dto.TransactionMethod,
            CreatedBy = dto.CreatedBy,
            CancelledAt = dto.CancelledAt,
            CancellationReason = dto.CancellationReason,
            PaymentMethodId = dto.PaymentMethodId,
            FineId = dto.FineId,
            ResourceId = dto.ResourceId,
            LibraryCardPackageId = dto.LibraryCardPackageId,
            PaymentMethod = dto.PaymentMethod,
            Fine = dto.Fine,
            LibraryResource = dto.LibraryResource,
            LibraryCardPackage = dto.LibraryCardPackage
        };
    }
}