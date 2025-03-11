using System.Text.Json.Serialization;

namespace FPTU_ELibrary.Domain.Entities;

public class UserFavorite
{
    public int FavoriteId { get; set; }

    public Guid UserId { get; set; }

    public int LibraryItemId { get; set; }

    public bool WantsToBorrow { get; set; }
    public bool WantsToBorrowAfterRequestFailed { get; set; }

    public DateTime CreatedAt { get; set; }
    
    public LibraryItem LibraryItem { get; set; } = null!;

    [JsonIgnore]
    public User User { get; set; } = null!;
}
