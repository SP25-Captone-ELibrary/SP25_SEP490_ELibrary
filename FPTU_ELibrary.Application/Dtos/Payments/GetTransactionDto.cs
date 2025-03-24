using System.Text.Json.Serialization;
using FPTU_ELibrary.Application.Dtos.Borrows;
using FPTU_ELibrary.Application.Dtos.Fine;
using FPTU_ELibrary.Application.Dtos.LibraryCard;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Application.Dtos.Payments;

public class GetTransactionDto
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
    
    // Payment method
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
    public List<BorrowRequestResourceDto> BorrowRequestResources { get; set; } = new ();
}

public static class GetTransactionDtoExtensions
{
    public static GetTransactionDto ToGetTransactionDto(this TransactionDto dto)
    {
        return new()
        {
            TransactionId = dto.TransactionId,
            TransactionCode = dto.TransactionCode,
            UserId = dto.UserId,
            Amount = dto.Amount,
            Description = dto.Description,
            TransactionStatus = dto.TransactionStatus,
            TransactionDate = dto.TransactionDate,
            TransactionMethod = dto.TransactionMethod,
            TransactionType = dto.TransactionType,
            ExpiredAt = dto.ExpiredAt,
            CreatedAt = dto.CreatedAt,
            CreatedBy = dto.CreatedBy,
            CancelledAt = dto.CancelledAt,
            CancellationReason = dto.CancellationReason,
            FineId = dto.FineId,
            ResourceId = dto.ResourceId,
            LibraryCardPackageId = dto.LibraryCardPackageId,
            PaymentMethodId = dto.PaymentMethodId,
            QrCode = dto.QrCode,
            User = dto.User != null! ? dto.User : null!,
            Fine = dto.Fine,
            LibraryResource = dto.LibraryResource,
            LibraryCardPackage = dto.LibraryCardPackage,
            PaymentMethod = dto.PaymentMethod,
            BorrowRequestResources = dto.BorrowRequestResources.Any()
                ? dto.BorrowRequestResources.ToList()
                : new()
        };
    }
}