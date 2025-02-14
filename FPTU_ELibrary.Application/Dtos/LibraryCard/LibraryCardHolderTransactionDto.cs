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
    public string? Description { get; set; } = null!;
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
    public UserDto User { get; set; } = null!;
    public PaymentMethodDto PaymentMethod { get; set; } = null!;
    public FineDto? Fine { get; set; }
    public DigitalBorrowDto? DigitalBorrow { get; set; }
    public LibraryCardPackageDto? LibraryCardPackage { get; set; }
    public InvoiceDto Invoice { get; set; } = null!;
}

public static class LibraryCardHolderTransactionExtensions
{
    public static LibraryCardHolderTransactionDto ToCardHolderTransactionDto(this TransactionDto dto)
    {
        return new()
        {
            TransactionId = dto.TransactionId,
            TransactionCode = dto.TransactionCode,
            UserId = dto.UserId,
            Amount = dto.Amount,
            Description = dto.Description,
            TransactionStatus = dto.TransactionStatus,
            TransactionType = dto.TransactionType,
            TransactionDate = dto.TransactionDate,
            CreatedAt = dto.CreatedAt,
            CancelledAt = dto.CancelledAt,
            CancellationReason = dto.CancellationReason,
            PaymentMethodId = dto.PaymentMethodId,
            FineId = dto.FineId,
            DigitalBorrowId = dto.DigitalBorrowId,
            LibraryCardPackageId = dto.LibraryCardPackageId,
            InvoiceId = dto.InvoiceId,
            PaymentMethod = dto.PaymentMethod,
            Fine = dto.Fine,
            DigitalBorrow = dto.DigitalBorrow,
            LibraryCardPackage = dto.LibraryCardPackage,
            Invoice = dto.Invoice,
        };
    }
}