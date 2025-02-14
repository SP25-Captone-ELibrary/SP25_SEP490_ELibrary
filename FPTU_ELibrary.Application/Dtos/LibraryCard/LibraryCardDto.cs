using System.Text.Json.Serialization;
using FPTU_ELibrary.Application.Dtos.Borrows;
using FPTU_ELibrary.Application.Dtos.Payments;
using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Application.Dtos.LibraryCard;

public class LibraryCardDto
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
    
    // Reissue & archived card
    public bool IsArchived { get; set; }
    public string? ArchiveReason { get; set; }
    public Guid? PreviousUserId { get; set; }
    
    // References
    public UserDto PreviousUser { get; set; } = null!;
    
    // Mapping entities
    [JsonIgnore]
    public ICollection<UserDto> Users { get; set; } = new List<UserDto>();
    
    [JsonIgnore] 
    public ICollection<BorrowRequestDto> BorrowRequests { get; set; } = new List<BorrowRequestDto>();
    
    [JsonIgnore] 
    public ICollection<BorrowRecordDto> BorrowRecords { get; set; } = new List<BorrowRecordDto>();
    
    [JsonIgnore]
    public ICollection<ReservationQueueDto> ReservationQueues { get; set; } = new List<ReservationQueueDto>();
}

public static class LibraryCardDtoExtensions
{
    public static LibraryCardDto AddPreviousUser(this LibraryCardDto libraryCardDto, UserDto? user)
    {
        if(user != null) libraryCardDto.PreviousUser = user;
        return libraryCardDto;
    }
}