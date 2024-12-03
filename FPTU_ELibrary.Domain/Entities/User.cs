using System.Text.Json.Serialization;

namespace FPTU_ELibrary.Domain.Entities;

public class User
{
    // Key
    public Guid UserId { get; set; }
    
    // User detail and credentials information
    public string? UserCode { get; set; }
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string? FirstName { get; set; } = null!;
    public string? LastName { get; set; } = null!;
    public DateTime? Dob { get; set; }
    public string? Phone { get; set; }
    public string? Avatar { get; set; }
    
    // Mark as active user or not 
    public bool IsActive { get; set; }
    
    //Identify the account's maker
    public string? ModifiedBy { get; set; }
    
    // Creation datetime
    public DateTime CreateDate { get; set; }

    // Multi-factor authentication
    public bool TwoFactorEnabled { get; set; }  
    public bool PhoneNumberConfirmed { get; set; }
    public bool EmailConfirmed { get; set; }
    public string? TwoFactorSecretKey { get; set; }
    public string? TwoFactorBackupCodes { get; set; }
    public string? PhoneVerificationCode { get; set; }
    public string? EmailVerificationCode { get; set; }
    public DateTime? PhoneVerificationExpiry { get; set; }

    // Role in the system
    public int RoleId { get; set; }

    // Mapping entities
    public SystemRole Role { get; set; } = null!;
    public ICollection<BorrowRecord> BorrowRecords { get; set; } = new List<BorrowRecord>();
    public ICollection<BorrowRequest> BorrowRequests { get; set; } = new List<BorrowRequest>();
    public ICollection<ReservationQueue> ReservationQueues { get; set; } = new List<ReservationQueue>();
    public ICollection<NotificationRecipient> NotificationRecipients { get; set; } = new List<NotificationRecipient>();


    [JsonIgnore]
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    [JsonIgnore]
    public ICollection<BookReview> BookReviews { get; set; } = new List<BookReview>();

    [JsonIgnore]
    public ICollection<UserFavorite> UserFavorites { get; set; } = new List<UserFavorite>();
}
