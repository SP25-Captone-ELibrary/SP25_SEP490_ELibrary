using System.Text.Json.Serialization;

namespace FPTU_ELibrary.Domain.Entities;

public class RefreshToken
{
    // Key
    public int Id { get; set; }

    // Refresh token ID
    public Guid RefreshTokenId { get; set; }

    // Creation and expiration datetime
    public DateTime CreateDate { get; set; }
    public DateTime ExpiryDate { get; set; }

    // For specific user
    public Guid UserId { get; set; }

    [JsonIgnore]
    public User User { get; set; } = null!;
}
