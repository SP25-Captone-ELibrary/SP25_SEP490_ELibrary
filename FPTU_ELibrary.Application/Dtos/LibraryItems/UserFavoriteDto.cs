using System.Text.Json.Serialization;

namespace FPTU_ELibrary.Application.Dtos.LibraryItems;

public class UserFavoriteDto
{
    public int FavoriteId { get; set; }
    
    public Guid UserId { get; set; }

    public int LibraryItemId { get; set; }
    
    public bool WantsToBorrow { get; set; }
    public bool WantsToBorrowAfterRequestFailed { get; set; }

    public DateTime CreatedAt { get; set; }
    
    public LibraryItemDto LibraryItem { get; set; } = null!;

    [JsonIgnore]
    public UserDto User { get; set; } = null!;
}