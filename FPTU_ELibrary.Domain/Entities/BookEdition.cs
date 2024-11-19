using System.Text.Json.Serialization;

namespace FPTU_ELibrary.Domain.Entities;

public class BookEdition
{
    // Key
    public int BookEditionId { get; set; }

    // Edition of which book
    public int BookId { get; set; }

    // Edition detail information
    public string EditionTitle { get; set; } = null!;
    public int EditionNumber { get; set; }
    public int PublicationYear { get; set; }
    public int PageCount { get; set; }
    public string Language { get; set; } = null!;
    public string? CoverImage { get; set; }
    public string? Format { get; set; }
    public string? Publisher { get; set; }
    public string Isbn { get; set; } = null!;
    public bool IsDeleted { get; set; }

	// Creation, update datetime and employee is charge of 
	public DateTime CreateDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public Guid CreateBy { get; set; }

    // Mapping entities
    [JsonIgnore]
    public Book Book { get; set; } = null!;
    public BookEditionInventory? BookEditionInventory { get; set; }
    public Employee CreateByNavigation { get; set; } = null!;

    public ICollection<BookEditionCopy> BookEditionCopies { get; set; } = new List<BookEditionCopy>();
    public ICollection<BookResource> BookResources { get; set; } = new List<BookResource>();
    public ICollection<BookReview> BookReviews { get; set; } = new List<BookReview>();

    [JsonIgnore]
    public ICollection<BorrowRequest> BorrowRequests { get; set; } = new List<BorrowRequest>();

    [JsonIgnore]
    public ICollection<ReservationQueue> ReservationQueues { get; set; } = new List<ReservationQueue>();

    [JsonIgnore]
    public ICollection<UserFavorite> UserFavorites { get; set; } = new List<UserFavorite>();
}
