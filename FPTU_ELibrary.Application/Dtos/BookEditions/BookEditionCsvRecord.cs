using CsvHelper.Configuration.Attributes;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Repositories.Base;

namespace FPTU_ELibrary.Application.Dtos.BookEditions;

public class BookEditionCsvRecord
{
    [Name("BookCode")] 
    public string BookCode { get; set; } = null!;

    [Name("CoverImage")]
    public string CoverImage { get; set; } = null!;

    [Name("EditionNumber")]
    public int EditionNumber { get; set; }

    [Name("Isbn")]
    public string Isbn { get; set; } = null!;

    [Name("EditionTitle")] 
    public string EditionTitle { get; set; } = null!;

    [Name("Summary")]
    public string Summary { get; set; } = null!;

    [Name("PublicationYear")]
    public int PublicationYear { get; set; }
    
    [Name("PageCount")]
    public int PageCount { get; set; }

    [Name("Language")] 
    public string Language { get; set; } = null!;
    
    [Name("Format")] 
    public string Format { get; set; } = null!;
    
    [Name("Publisher")] 
    public string Publisher { get; set; } = null!;

    [Name("EstimatedPrice")] 
    public decimal EstimatedPrice { get; set; }

    [Name("ShelfNumber")] 
    public string ShelfNumber { get; set; } = null!;

    [Name("EditionCopyBarcodes")] 
    public string? EditionCopyBarcodes { get; set; } = null!;
    
    [Name("AuthorCodes")] 
    public string AuthorCodes { get; set; } = null!;
    
    [Name("Categories")] 
    public string Categories { get; set; } = null!;
}

public static class BookEditionCsvRecordExtensions
{
    public static List<BookEditionCsvRecord> ToBookEditionCsvRecords(this List<BookEditionDto> bookEditions)
        => bookEditions.Select(be => new BookEditionCsvRecord()
        {
            BookCode = be.Book.BookCode,
            CoverImage = be.CoverImage ?? null!,
            EditionNumber = be.EditionNumber,
            EditionTitle = be.EditionTitle ?? null!,
            Summary = be.EditionSummary ?? null!,
            Publisher = be.Publisher ?? null!,
            Language = be.Language,
            PageCount = be.PageCount,
            PublicationYear = be.PublicationYear,
            Format = be.Format ?? null!,
            Isbn = be.Isbn,
            EstimatedPrice = be.EstimatedPrice,
            ShelfNumber = be.Shelf?.ShelfNumber ?? null!,
            Categories = String.Join(", ", be.Book.BookCategories.Select(bc => bc.Category).Select(c => c.EnglishName).ToList()),
            AuthorCodes = String.Join("\n", be.BookEditionAuthors.Select(bea => bea.Author).Select(a => a.AuthorCode).ToList()),
            EditionCopyBarcodes = String.Join("\n", be.BookEditionCopies.Select(bec => bec.Barcode).ToList())
        }).ToList();
}