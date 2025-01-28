using System.Text.Json.Serialization;

namespace FPTU_ELibrary.Domain.Entities;

public class LibraryItemReview
{
    // Key
    public int ReviewId { get; set; }

    // Review for which edition
    public int LibraryItemId { get; set; }

    // Who review
    public Guid UserId { get; set; }

    // Review content
    public double RatingValue { get; set; }
    public string? ReviewText { get; set; }

    // Creation and update datetime
    public DateTime CreateDate { get; set; }
    public DateTime? UpdatedDate { get; set; }

    // Mapping entities
    [JsonIgnore]
    public LibraryItem LibraryItem { get; set; } = null!;

    [JsonIgnore]
    public User User { get; set; } = null!;
}
