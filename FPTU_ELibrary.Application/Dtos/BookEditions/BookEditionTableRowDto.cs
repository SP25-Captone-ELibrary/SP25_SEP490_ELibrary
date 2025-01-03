using FPTU_ELibrary.Application.Dtos.Books;
using FPTU_ELibrary.Application.Dtos.Locations;

namespace FPTU_ELibrary.Application.Dtos.BookEditions;

public class BookEditionTableRowDto
{
    public int BookId { get; set; }
    public int BookEditionId { get; set; }
    public int EditionNumber { get; set; }
    public int PublicationYear { get; set; }
    public int PageCount { get; set; }
    public int TotalCopies { get; set; }
    public int AvailableCopies { get; set; }
    public int BorrowedCopies { get; set; }
    public int RequestCopies { get; set; }
    public int ReservedCopies { get; set; }
    public string Author { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string? EditionTitle { get; set; }
    public string? Shelf { get; set; }
    public string? Format { get; set; } 
    public string Isbn { get; set; } = null!;
    public string Language { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string? CoverImage { get; set; }
    public string? Publisher { get; set; }
    public string CreateBy { get; set; } = null!;
    public bool CanBorrow { get; set; }
    public DateTime CreatedAt { get; set; } 
    public DateTime? UpdatedAt { get; set; }
    public List<CategoryDto> Categories { get; set; } = null!;
}

public static class BookEditionTableRowDtoExtensions
{
    public static List<BookEditionTableRowDto> ToEditionTableRows(this List<BookEditionDto> dtos)
    {
        return dtos.Select(be => new BookEditionTableRowDto()
        {
            BookId = be.BookId,
            Title = be.Book.Title,
            EditionTitle = be.EditionTitle,
            BookEditionId = be.BookEditionId,
            EditionNumber = be.EditionNumber,
            PublicationYear = be.PublicationYear,
            PageCount = be.PageCount,
            Shelf = be.Shelf?.ShelfNumber,
            Format = be.Format,
            Isbn = be.Isbn,
            Language = be.Language,
            Status = be.Status.ToString(),
            CoverImage = be.CoverImage,
            Publisher = be.Publisher,
            CreateBy = be.CreatedBy,
            CanBorrow = be.CanBorrow,
            CreatedAt = be.CreatedAt,
            UpdatedAt = be.UpdatedAt,
            Categories = be.Book.BookCategories.Select(x => x.Category).ToList(),
            Author = String.Join(" & ", be.BookEditionAuthors.Select(bea => bea.Author.FullName)),
            TotalCopies = be.BookEditionInventory?.TotalCopies ?? 0,
            AvailableCopies = be.BookEditionInventory?.AvailableCopies ?? 0,
            RequestCopies = be.BookEditionInventory?.RequestCopies ?? 0,
            BorrowedCopies = be.BookEditionInventory?.BorrowedCopies ?? 0,
            ReservedCopies = be.BookEditionInventory?.ReservedCopies ?? 0,
        }).ToList();
    }
}