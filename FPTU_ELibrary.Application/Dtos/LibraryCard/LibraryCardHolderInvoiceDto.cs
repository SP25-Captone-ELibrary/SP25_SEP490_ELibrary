using FPTU_ELibrary.Application.Dtos.Payments;
using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Application.Dtos.LibraryCard;

public class LibraryCardHolderInvoiceDto
{
    public int InvoiceId { get; set; }
    public Guid UserId { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public ICollection<TransactionDto> Transactions { get; set; } = new List<TransactionDto>();
}

public static class LibraryCardHolderInvoiceDtoExtensions
{
    public static LibraryCardHolderInvoiceDto ToCardHolderInvoiceDto(this InvoiceDto dto)
    {
        return new()
        {
            InvoiceId = dto.InvoiceId,   
            UserId = dto.UserId,   
            TotalAmount = dto.TotalAmount,   
            CreatedAt = dto.CreatedAt,   
            PaidAt = dto.PaidAt,
            Transactions = dto.Transactions.Any()
                ? dto.Transactions.Select(trans => new TransactionDto()
                {
                    TransactionId = trans.TransactionId,
                    TransactionCode = trans.TransactionCode,
                    UserId = trans.UserId,
                    Amount = trans.Amount,
                    Description = trans.Description,
                    TransactionStatus = trans.TransactionStatus,
                    TransactionType = trans.TransactionType,
                    TransactionDate = trans.TransactionDate,
                    CreatedAt = trans.CreatedAt,
                    CancelledAt = trans.CancelledAt,
                    CancellationReason = trans.CancellationReason,
                    PaymentMethodId = trans.PaymentMethodId,
                    FineId = trans.FineId,
                    DigitalBorrowId = trans.DigitalBorrowId,
                    LibraryCardPackageId = trans.LibraryCardPackageId,
                    InvoiceId = trans.InvoiceId,
                    PaymentMethod = trans.PaymentMethod
                }).ToList()
                : new()
        };
    }
}