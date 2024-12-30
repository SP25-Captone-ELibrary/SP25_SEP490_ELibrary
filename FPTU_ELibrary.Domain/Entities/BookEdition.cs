using System.Text.Json.Serialization;
using FPTU_ELibrary.Domain.Interfaces;

namespace FPTU_ELibrary.Domain.Entities;

public class BookEdition : IAuditableEntity
{
    // Key
    public int BookEditionId { get; set; }

    // Edition of which book
    public int BookId { get; set; }

    // Edition detail information
    public string? EditionTitle { get; set; }
    public string? EditionSummary { get; set; }
    public int EditionNumber { get; set; }
    public int PageCount { get; set; }
    public string Language { get; set; } = null!;
	public int PublicationYear { get; set; }
    public string? CoverImage { get; set; }
    public string? Format { get; set; }
    public string? Publisher { get; set; }
    public string Isbn { get; set; } = null!;
    public bool IsDeleted { get; set; }
    public bool CanBorrow { get; set; }
    public decimal EstimatedPrice { get; set; }
    
    // Locate in which shelf
    public int? ShelfId { get; set; }
    
	// Creation, update datetime and employee is charge of 
	public DateTime CreatedAt { get; set; }
	public DateTime? UpdatedAt { get; set; }
	public string CreatedBy { get; set; } = null!;
	public string? UpdatedBy { get; set; }

    // Mapping entities
    [JsonIgnore]
    public Book Book { get; set; } = null!;
    public LibraryShelf? Shelf { get; set; }
    public BookEditionInventory? BookEditionInventory { get; set; }

    public ICollection<BookEditionAuthor> BookEditionAuthors { get; set; } = new List<BookEditionAuthor>();
    public ICollection<BookEditionCopy> BookEditionCopies { get; set; } = new List<BookEditionCopy>();
    public ICollection<BookReview> BookReviews { get; set; } = new List<BookReview>();
    
    [JsonIgnore]
    public ICollection<BorrowRequest> BorrowRequests { get; set; } = new List<BorrowRequest>();

    [JsonIgnore]
    public ICollection<ReservationQueue> ReservationQueues { get; set; } = new List<ReservationQueue>();

    [JsonIgnore]
    public ICollection<UserFavorite> UserFavorites { get; set; } = new List<UserFavorite>();
}
