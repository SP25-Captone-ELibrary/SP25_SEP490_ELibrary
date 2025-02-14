using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Application.Dtos.Payments;
using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Application.Dtos.LibraryCard;

public class LibraryCardHolderDigitalBorrowDto
{
    public int DigitalBorrowId { get; set; }
    public int ResourceId { get; set; }
    public Guid UserId { get; set; }
    public DateTime RegisterDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public bool IsExtended { get; set; }
    public int ExtensionCount { get; set; }
    public BorrowDigitalStatus Status { get; set; }

    public LibraryResourceDto LibraryResource { get; set; } = null!;
    public List<TransactionDto> Transactions { get; set; } = new();
}

public static class LibraryCardHolderDigitalBorrowExtensions
{
    public static LibraryCardHolderDigitalBorrowDto ToCardHolderDigitalBorrowDto(this DigitalBorrowDto dto)
    {
        return new()
        {
            DigitalBorrowId = dto.DigitalBorrowId,
            ResourceId = dto.ResourceId,
            UserId = dto.UserId,
            RegisterDate = dto.RegisterDate,
            ExpiryDate = dto.ExpiryDate,
            IsExtended = dto.IsExtended,
            ExtensionCount = dto.ExtensionCount,
            Status = dto.Status,
            LibraryResource = dto.LibraryResource,
            Transactions = dto.Transactions.ToList()
        };
    }
}