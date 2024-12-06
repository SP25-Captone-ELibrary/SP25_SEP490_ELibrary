using System.Text.Json.Serialization;
using FPTU_ELibrary.Domain.Entities.Base;

namespace FPTU_ELibrary.Domain.Entities;

public class User : BaseUser
{
    // Key
    public Guid UserId { get; set; }
    
    // User detail and credentials information
    public string? UserCode { get; set; }

    // Role in the system
    public int RoleId { get; set; }

    public string? ModifiedBy { get; set; }

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
