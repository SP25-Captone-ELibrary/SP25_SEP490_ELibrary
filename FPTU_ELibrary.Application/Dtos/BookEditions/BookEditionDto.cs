using System.Text.Json.Serialization;
using FPTU_ELibrary.Application.Dtos.Books;
using FPTU_ELibrary.Application.Dtos.Locations;
using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Application.Dtos.BookEditions;

public class BookEditionDto
{
    // Key
    public int BookEditionId { get; set; }
    
    // Edition of which book
    public int BookId { get; set; }

    // Edition detail information
    public string? EditionTitle { get; set; }
    public string? EditionSummary { get; set; }
    public int EditionNumber { get; set; }
    public int PublicationYear { get; set; }
    public int PageCount { get; set; }
    public string Language { get; set; } = null!;
    public string? CoverImage { get; set; }
    public string? Format { get; set; }
    public string? Publisher { get; set; }
    public string Isbn { get; set; } = null!;
    public bool IsDeleted { get; set; }
    public bool CanBorrow { get; set; }
    public decimal EstimatedPrice { get; set; }
    
    // Locate in which shelf
    public int? ShelfId { get; set; }
    
    // Edition status (Draft, Published)
    public BookEditionStatus Status { get; set; }

    // Creation, update datetime and employee is charge of 
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = null!;
    public string? UpdatedBy { get; set; }

    // AI Training fields
    public bool IsTrained { get; set; }
    public DateTime? TrainedDay { get; set; }
    
    // Mapping entities
    [JsonIgnore]
    public BookDto Book { get; set; } = null!;
    public LibraryShelfDto? Shelf { get; set; }
    public BookEditionInventoryDto? BookEditionInventory { get; set; }
    
    public ICollection<BookEditionAuthorDto> BookEditionAuthors { get; set; } = new List<BookEditionAuthorDto>();
    public ICollection<BookEditionCopyDto> BookEditionCopies { get; set; } = new List<BookEditionCopyDto>();
    public ICollection<BookReviewDto> BookReviews { get; set; } = new List<BookReviewDto>();
    
    // [JsonIgnore]
    // public ICollection<BorrowRequest> BorrowRequests { get; set; } = new List<BorrowRequest>();

    // [JsonIgnore]
    // public ICollection<ReservationQueue> ReservationQueues { get; set; } = new List<ReservationQueue>();

    // [JsonIgnore]
    // public ICollection<UserFavorite> UserFavorites { get; set; } = new List<UserFavorite>();
}