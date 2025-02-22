using CsvHelper.Configuration.Attributes;

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
    public string? PhysicalDetails { get; set; } = null!;
    
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
    
    [Name("ĐCKB Bản Sao")] 
    public string? ItemInstanceBarcodes { get; set; }
    
    [Name("Mã Tác Giả")] 
    public string? AuthorCode { get; set; }

    [Name("Phân Loại")] 
    public string Category { get; set; } = null!;
}

public static class BookEditionCsvRecordExtensions
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
            AuthorCode = String.Join(", ", be.LibraryItemAuthors.Select(bea => bea.Author).Select(a => a.AuthorCode).ToList()),
            ItemInstanceBarcodes = String.Join(", ", be.LibraryItemInstances.Select(bec => bec.Barcode).ToList())
        }).ToList();
}