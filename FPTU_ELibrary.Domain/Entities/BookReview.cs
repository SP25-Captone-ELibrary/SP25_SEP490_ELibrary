using System.Text.Json.Serialization;

namespace FPTU_ELibrary.Domain.Entities;

public class BookReview
{
    // Key
    public int ReviewId { get; set; }

    // Review for which edition
    public int BookEditionId { get; set; }

    // Who review
    public Guid UserId { get; set; }

    // Review content
    public int RatingValue { get; set; }
    public string? ReviewText { get; set; }

    // Creation and update datetime
    public DateTime CreateDate { get; set; }
    public DateTime? UpdatedDate { get; set; }

    // Mapping entities
    [JsonIgnore]
    public BookEdition BookEdition { get; set; } = null!;

    [JsonIgnore]
    public User User { get; set; } = null!;
}
