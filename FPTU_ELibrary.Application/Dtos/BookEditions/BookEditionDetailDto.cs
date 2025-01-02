using FPTU_ELibrary.Application.Dtos.Authors;
using FPTU_ELibrary.Application.Dtos.Locations;

namespace FPTU_ELibrary.Application.Dtos.BookEditions;

public class BookEditionDetailDto
{
    // Book information
    public int BookId { get; set; }
    public string Title { get; set; } = null!;
    public string? SubTitle { get; set; }
    public string? Summary { get; set; }
    public DateTime BookCreateDate { get; set; }
    public DateTime? BookUpdatedDate { get; set; }
    public bool IsTrained { get; set; } = false;
    public DateTime? TrainedDay { get; set; }
    // Edition detail information
    public int BookEditionId { get; set; }
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

    // Shelf
    public LibraryShelfDto? Shelf { get; set; }
    // Inventory
    public BookEditionInventoryDto? BookEditionInventory { get; set; }
    
    // Authors
    public List<AuthorDto> Authors { get; set; } = new();
    // Copies
    public List<BookEditionCopyDto> BookEditionCopies { get; set; } = new();
}

public static class BookEditionDetailDtoExtensions
{
    public static BookEditionDetailDto ToEditionDetailDto(this BookEditionDto dto)
    {
        return new BookEditionDetailDto()
        {
            // Book information
            BookId = dto.BookId,
            Title = dto.Book != null! ?  dto.Book.Title : null!,
            SubTitle = dto.Book != null! ?  dto.Book.SubTitle : null!,
            Summary = dto.Book != null! ?  dto.Book.Summary : null!,
            
            // Book edition information
            BookEditionId = dto.BookEditionId,
            EditionTitle = dto.EditionTitle,
            EditionNumber = dto.EditionNumber,
            EditionSummary = dto.EditionSummary,
            PublicationYear = dto.PublicationYear,
            PageCount = dto.PageCount,
            Language = dto.Language,
            Format = dto.Format,
            CoverImage = dto.CoverImage,
            Publisher = dto.Publisher,
            Isbn = dto.Isbn,
            IsDeleted = dto.IsDeleted,
            CanBorrow = dto.CanBorrow,
            EstimatedPrice = dto.EstimatedPrice,
            IsTrained = dto.IsTrained,
            TrainedDay = dto.TrainedDay,
            
            // Shelf information
            ShelfId = dto.ShelfId,
            Shelf = dto.Shelf,
            
            // Inventory 
            BookEditionInventory = dto.BookEditionInventory,
            
            // Authors
            // Authors = dto.BookEditionAuthors.Any() ? dto.BookEditionAuthors.Select(bea => bea.Author).ToList() : new(),
            Authors = dto.BookEditionAuthors.Select(bea => bea.Author).ToList(),
            
            // Edition copies
            BookEditionCopies = dto.BookEditionCopies.ToList(),
        };
    }
    
    public static BookEditionDetailDto ToEditionDetailDtoWithBookDetail(this BookEditionDto dto,
        string title, string? subTitle, string? summary)
    {
        return new BookEditionDetailDto()
        {
            // Book information
            BookId = dto.BookId,
            Title = title,
            SubTitle = subTitle,
            Summary = summary,
            
            // Book edition information
            BookEditionId = dto.BookEditionId,
            EditionTitle = dto.EditionTitle,
            EditionNumber = dto.EditionNumber,
            EditionSummary = dto.EditionSummary,
            PublicationYear = dto.PublicationYear,
            PageCount = dto.PageCount,
            Language = dto.Language,
            Format = dto.Format,
            CoverImage = dto.CoverImage,
            Publisher = dto.Publisher,
            Isbn = dto.Isbn,
            IsDeleted = dto.IsDeleted,
            CanBorrow = dto.CanBorrow,
            EstimatedPrice = dto.EstimatedPrice,
            IsTrained = dto.IsTrained,
            TrainedDay = dto.TrainedDay,
            
            // Shelf information
            ShelfId = dto.ShelfId,
            Shelf = dto.Shelf,
            
            // Inventory 
            BookEditionInventory = dto.BookEditionInventory,
            
            // Authors
            Authors = dto.BookEditionAuthors.Select(bea => bea.Author).ToList(),
            
            // Edition copies
            BookEditionCopies = dto.BookEditionCopies.ToList(),
        };
    }
}