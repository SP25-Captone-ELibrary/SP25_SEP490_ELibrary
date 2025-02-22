using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.API.Payloads.Requests.LibraryCard;

public class UpdateLibraryCardRequest
{
    public string FullName { get; set; } = null!;
    public string Avatar { get; set; } = null!;
    public LibraryCardIssuanceMethod IssuanceMethod { get; set; }
    
    // Extend borrow amount (only employee can update this field, user may send email
    // or contact directly to library to describe for reason)
    public bool IsAllowBorrowMore { get; set; } // This field will automatically change to false when first create borrow request after employee updated (already handled in Borrow feature)  
    public int MaxItemOnceTime { get; set; } // Employee will update total item can be borrowed once time for user
    public string? AllowBorrowMoreReason { get; set; }
    
    // Total missed pick up (not pick up after create request)
    public int TotalMissedPickUp { get; set; }
}