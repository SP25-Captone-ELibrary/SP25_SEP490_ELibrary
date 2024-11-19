using System.Text.Json.Serialization;

namespace FPTU_ELibrary.Domain.Entities;

public class UserFavorite
{
    public int FavoriteId { get; set; }

    public Guid UserId { get; set; }

    public int BookEditionId { get; set; }

    public BookEdition BookEdition { get; set; } = null!;

    [JsonIgnore]
    public User User { get; set; } = null!;
}
