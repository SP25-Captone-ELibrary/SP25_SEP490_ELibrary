using FPTU_ELibrary.Application.Dtos.Payments;
using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Application.Dtos.LibraryCard;

public class GetLibraryCardDetailDto
{
    // Key
    public Guid LibraryCardId { get; set; }
    
    // Library card information
    public string FullName { get; set; } = null!;
    public string Avatar { get; set; } = null!;
    public string Barcode { get; set; } = null!;
    
    // Issue method: in-person | online & request status
    public LibraryCardIssuanceMethod IssuanceMethod { get; set; } 
    
    // Library card status
    public LibraryCardStatus Status { get; set; } 

    // Extend borrow amount (only employee can update this field, user may send email
    // or contact directly to library to describe for reason)
    public bool IsAllowBorrowMore { get; set; } // This field will automatically change to false when first create borrow request after employee updated (already handled in Borrow feature)  
    public int MaxItemOnceTime { get; set; } // Employee will update total item can be borrowed once time for user
    public string? AllowBorrowMoreReason { get; set; } // Reason why increase the total borrow amount
    
    // Total missed pick up (not pick up after create request)
    public int TotalMissedPickUp { get; set; }
    
    // Remind user before expiration (via email or system notification)
    public bool IsReminderSent { get; set; }
    
    // Issue and expiry date
    public bool IsExtended { get; set; }
    public int ExtensionCount { get; set; }
    public DateTime IssueDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public DateTime? SuspensionEndDate { get; set; }
    public string? SuspensionReason { get; set; } 
    
    // Reject reason
    public string? RejectReason { get; set; }
    
    // Reissue & archived card
    public bool IsArchived { get; set; }
    public string? ArchiveReason { get; set; }
    public Guid? PreviousUserId { get; set; }
    
    // Payment information
    public string? TransactionCode { get; set; }
    
    // References
    public UserDto PreviousUser { get; set; } = null!;

    public List<TransactionDto> Transactions { get; set; } = new();
}

public static class GetLibraryCardDetailDtoExtensions
{
    public static GetLibraryCardDetailDto ToGetLibraryCardDetailDto(this LibraryCardDto dto,
        List<TransactionDto>? transactions)
        => new()
        {
            LibraryCardId = dto.LibraryCardId,
            FullName = dto.FullName,
            Avatar = dto.Avatar,
            Barcode = dto.Barcode,
            IssuanceMethod = dto.IssuanceMethod,
            Status = dto.Status,
            IsAllowBorrowMore = dto.IsAllowBorrowMore,
            MaxItemOnceTime = dto.MaxItemOnceTime,
            AllowBorrowMoreReason = dto.AllowBorrowMoreReason,
            TotalMissedPickUp = dto.TotalMissedPickUp,
            IsReminderSent = dto.IsReminderSent,
            IsExtended = dto.IsExtended,
            ExtensionCount = dto.ExtensionCount,
            IssueDate = dto.IssueDate,
            ExpiryDate = dto.ExpiryDate,
            SuspensionEndDate = dto.SuspensionEndDate,
            SuspensionReason = dto.SuspensionReason,
            RejectReason = dto.RejectReason,
            IsArchived = dto.IsArchived,
            ArchiveReason = dto.ArchiveReason,
            PreviousUserId = dto.PreviousUserId,
            TransactionCode = dto.TransactionCode,
            PreviousUser = dto.PreviousUser,
            Transactions = transactions != null && transactions.Any()
                ? transactions : new()
        };
}