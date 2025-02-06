using System.Text.Json.Serialization;
using FPTU_ELibrary.Domain.Entities.Base;

namespace FPTU_ELibrary.Domain.Entities;

public class User : BaseUser
{
    // Key
    public Guid UserId { get; set; }
    
    // Role in the system
    public int RoleId { get; set; }
    
    // Library card information
    public Guid? LibraryCardId { get; set; }
    
    // Mapping entities
    public SystemRole Role { get; set; } = null!;
    public LibraryCard? LibraryCard { get; set; } 
    
    public ICollection<NotificationRecipient> NotificationRecipients { get; set; } = new List<NotificationRecipient>();

    [JsonIgnore]
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    
    [JsonIgnore]
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    
    [JsonIgnore]
    public ICollection<DigitalBorrow> DigitalBorrows { get; set; } = new List<DigitalBorrow>();
    
    [JsonIgnore]
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    [JsonIgnore]
    public ICollection<LibraryItemReview> LibraryItemReviews { get; set; } = new List<LibraryItemReview>();

    [JsonIgnore]
    public ICollection<UserFavorite> UserFavorites { get; set; } = new List<UserFavorite>();
}
