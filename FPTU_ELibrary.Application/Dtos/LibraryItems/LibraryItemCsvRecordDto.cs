using CsvHelper.Configuration.Attributes;
using FPTU_ELibrary.Application.Dtos.Authors;
using FPTU_ELibrary.Application.Dtos.Locations;
using FPTU_ELibrary.Application.Utils;
using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Application.Dtos.LibraryItems;

public class LibraryItemCsvRecordDto
{
    [Name("Bìa Sách")]
    public string CoverImage { get; set; } = null!;

    [Name("Tiêu Đề")] 
    public string Title { get; set; } = null!;
    
    [Name("Tiêu Đề Phụ")] 
    public string? SubTitle { get; set; }
    
    [Name("Thứ Tự Ấn Bản")] 
    public int? EditionNumber { get; set; } 
    
    [Name("Ấn bản")] 
    public string? Edition { get; set; }
    
    [Name("DDC")] 
    public string ClassificationNumber { get; set; } = null!;
    
    [Name("Ký Hiệu Xếp Giá")] 
    public string CutterNumber { get; set; } = null!;
    
    [Name("ISBN")] 
    public string? Isbn { get; set; } 
    
    [Name("Tóm tắt")] 
    public string? Summary { get; set; }
    
    [Name("Thể loại")] 
    public string? Genres { get; set; } 
    
    [Name("Từ khóa")] 
    public string? TopicalTerms { get; set; } 
    
    [Name("Thông Tin Trách Nhiệm")] 
    public string? Responsibility { get; set; } 
    
    [Name("Tác Giả Bổ Sung")] 
    public string? AdditionalAuthors { get; set; } 
    
    [Name("Năm Xuất Bản")] 
    public int PublicationYear { get; set; }
    
    [Name("Trang")] 
    public int? PageCount { get; set; }
    
    [Name("Ngôn Ngữ")] 
    public string Language { get; set; } = null!;
    
    [Name("Ngôn Ngữ Gốc")] 
    public string? OriginLanguage { get; set; } = null!;
    
    [Name("Nhà Xuất Bản")] 
    public string Publisher { get; set; } = null!;
    
    [Name("Nơi Xuất Bản")] 
    public string PublicationPlace { get; set; } = null!;
    
    [Name("Mô Tả Vật Lý")] 
    public string? PhysicalDetails { get; set; }
    
    [Name("Khổ")] 
    public string Dimensions { get; set; } = null!;
    
    [Name("Giá")] 
    public decimal? EstimatedPrice { get; set; } 
    
    [Name("Phụ Chú Chung")] 
    public string? GeneralNote { get; set; }  
    
    [Name("Phụ Chú Thư Mục")] 
    public string? BibliographicalNote { get; set; } 
    
    [Name("Số Kệ")] 
    public string? ShelfNumber { get; set; } 
    
    [Name("Mã Tác Giả")] 
    public string? AuthorCode { get; set; }

    [Name("Phân Loại")] 
    public string Category { get; set; } = null!;
}

public static class LibraryItemCsvRecordExtensions
{
    public static List<LibraryItemCsvRecordDto> ToLibraryItemCsvRecords(this List<LibraryItemDto> items)
        => items.Select(be => new LibraryItemCsvRecordDto()
        {
            CoverImage = be.CoverImage ?? null!,
            EditionNumber = be.EditionNumber,
            Edition = be.Edition,
            Title = be.Title,
            SubTitle = be.SubTitle,
            ClassificationNumber = be.ClassificationNumber ?? string.Empty,
            CutterNumber = be.CutterNumber ?? string.Empty,
            Publisher = be.Publisher ?? null!,
            Summary = be.Summary,
            Genres = be.Genres,
            TopicalTerms = be.TopicalTerms,
            Responsibility = be.Responsibility,
            AdditionalAuthors = be.AdditionalAuthors,
            PublicationYear = be.PublicationYear,
            PageCount = be.PageCount,
            Language = be.Language,
            OriginLanguage = be.OriginLanguage ?? null!,
            PublicationPlace = be.PublicationPlace ?? null!,
            PhysicalDetails = be.PhysicalDetails,
            Dimensions = be.Dimensions ?? null!,
            GeneralNote = be.GeneralNote,
            BibliographicalNote = be.BibliographicalNote,
            EstimatedPrice = be.EstimatedPrice,
            Isbn = be.Isbn,
            ShelfNumber = be.Shelf?.ShelfNumber ?? null!,
            Category = be.Category.VietnameseName,
            AuthorCode = String.Join(", ", be.LibraryItemAuthors.Select(bea => bea.Author).Select(a => a.AuthorCode).ToList())
        }).ToList();

    public static LibraryItemDto ToLibraryItemDto(
        this LibraryItemCsvRecordDto record,
        Dictionary<string, string> imageUrlDic,
        List<CategoryDto> categories,
        List<LibraryShelfDto>? shelves,
        List<AuthorDto>? authors)
    {
        // Try to get library shelf
        var shelf = shelves?.FirstOrDefault(s => Equals(s.ShelfNumber, record.ShelfNumber));
        // Try to get category
        var category = categories.FirstOrDefault(c =>
            Equals(c.EnglishName.ToLower(), record.Category.ToLower()) || 
            Equals(c.VietnameseName.ToLower(), record.Category.ToLower()));
        
        // Try to initialize authors (if any)
        var authorCodes = record.AuthorCode?.Split(",").Select(str => str.Trim()).ToArray();
        var libItemAuthors = authorCodes != null && authorCodes.Any() && authors != null && authors.Any()
            ? authorCodes.Select(ac =>
            {
                var author = authors.FirstOrDefault(a => Equals(a.AuthorCode, ac));
                if (author != null)
                {
                    return new LibraryItemAuthorDto()
                    {
                        AuthorId = author.AuthorId
                    };
                }

                return null;
            }).ToList()
            : new List<LibraryItemAuthorDto?>();
        
        // Initialize and mapping library item dto 
        var libRes = new LibraryItemDto()
        {
            // Cover image
            CoverImage = imageUrlDic.TryGetValue(record.CoverImage, out var coverImageUrl)
                ? coverImageUrl
                : null,
            EditionNumber = record.EditionNumber,
            Edition = record.Edition,
            Title = record.Title,
            SubTitle = record.SubTitle,
            ClassificationNumber = record.ClassificationNumber,
            CutterNumber = record.CutterNumber,
            Publisher = record.Publisher,
            Summary = record.Summary,
            Genres = record.Genres,
            TopicalTerms = record.TopicalTerms,
            Responsibility = record.Responsibility,
            AdditionalAuthors = record.AdditionalAuthors,
            PublicationYear = record.PublicationYear,
            PageCount = record.PageCount,
            Language = record.Language,
            OriginLanguage = record.OriginLanguage,
            PublicationPlace = record.PublicationPlace,
            PhysicalDetails = record.PhysicalDetails,
            Dimensions = record.Dimensions,
            GeneralNote = record.GeneralNote,
            BibliographicalNote = record.BibliographicalNote,
            EstimatedPrice = record.EstimatedPrice,
            Isbn = record.Isbn != null ? ISBN.CleanIsbn(record.Isbn) : null,
            // Default values
            IsTrained = false,
            IsDeleted = false,
            CanBorrow = false,
            Status = LibraryItemStatus.Draft,
            // Shelf
            ShelfId = shelf?.ShelfId,
            // Inventory
            LibraryItemInventory = new()
            {
                TotalUnits = 0,
                AvailableUnits = 0,
                RequestUnits = 0,
                ReservedUnits = 0,
                BorrowedUnits = 0
            },
            // Authors
            LibraryItemAuthors = libItemAuthors.Any(a => a != null) 
                ? libItemAuthors.Where(a => a != null).ToList()!
                : new List<LibraryItemAuthorDto>()
        }; 
        
        // Only assign category when exist
        if(category != null) libRes.CategoryId = category.CategoryId;

        return libRes;
    }
}