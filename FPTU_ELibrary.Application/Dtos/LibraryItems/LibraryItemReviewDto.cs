using System.Text.Json.Serialization;

namespace FPTU_ELibrary.Application.Dtos.LibraryItems;

public class LibraryItemReviewDto
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
    public LibraryItemDto LibraryItem { get; set; } = null!;

    public UserDto User { get; set; } = null!;
}