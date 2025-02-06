using System.Text.Json.Serialization;
using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Domain.Entities;

public class LibraryCard
{
    // Key
    public Guid LibraryCardId { get; set; }
    
    // Library card information
    public string FullName { get; set; } = null!;
    public string Avatar { get; set; } = null!;
    public string Barcode { get; set; } = null!;
    
    // Issue method: in-person | online & request status
    public LibraryCardIssuanceMethod IssuanceMethod { get; set; } 
    public LibraryCardStatus Status { get; set; } 

    // Extend borrow amount (only employee can update this field, user may send email
    // or contact directly to library to describe for reason)
    public bool IsAllowBorrowMore { get; set; } // This field will automatically change to false when first create borrow request after employee updated (already handled in Borrow feature)  
    public int MaxItemOnceTime { get; set; } // Employee will update total item can be borrowed once time for user
    
    // Remind user before expiration (via email or system notification)
    public bool IsReminderSent { get; set; }
    
    // Total missed pick up (not pick up after create request) -> change status to suspended if > specific value
    public int TotalMissedPickUp { get; set; }
    
    // Issue and expiry date
    public bool IsExtended { get; set; }
    public int ExtensionCount { get; set; }
    public DateTime IssueDate { get; set; }
    public DateTime? ExpiryDate { get; set; } // Indicates if the expiry date has been extended
    public DateTime? SuspensionEndDate { get; set; } // Number of times the borrow period has been extended
    
    // Mapping entities
    [JsonIgnore]
    public ICollection<User> Users { get; set; } = new List<User>();
    
    [JsonIgnore] 
    public ICollection<BorrowRequest> BorrowRequests { get; set; } = new List<BorrowRequest>();
    
    [JsonIgnore] 
    public ICollection<BorrowRecord> BorrowRecords { get; set; } = new List<BorrowRecord>();
    
    [JsonIgnore]
    public ICollection<ReservationQueue> ReservationQueues { get; set; } = new List<ReservationQueue>();
}