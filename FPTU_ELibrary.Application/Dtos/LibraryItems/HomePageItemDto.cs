using FPTU_ELibrary.Application.Dtos.Authors;
using FPTU_ELibrary.Application.Dtos.Locations;
using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Application.Dtos.LibraryItems;

public class HomePageItemDto
{
   // Key
    public int LibraryItemId { get; set; }

    // Item MARC21 details
    public string Title { get; set; } = null!; // Title: [245a]
    public string? SubTitle { get; set; } // Subtitle: [245b]
    public string? Responsibility { get; set; } // Responsibility: [245c]
    public string? Edition { get; set; } // Edition/Reprint: [250a]
    public int? EditionNumber { get; set; } // Edition number
    public string Language { get; set; } = null!; // Language: [041a]
    public string? OriginLanguage { get; set; } // Original language: [041h]
    public string? Summary { get; set; } // Summary: [520a]
    public string? CoverImage { get; set; } // Cover image
    public int PublicationYear { get; set; } // Publication year: [260c]
    public string? Publisher { get; set; } // Publisher: [260b]
    public string? PublicationPlace { get; set; } // Publication place: [260a]
    public string ClassificationNumber { get; set; } = null!; // Classification number: [82a]
    public string CutterNumber { get; set; } = null!; // Cutter number: [82b]
    public string? Isbn { get; set; } // ISBN: [20a]
    public string? Ean { get; set; } //  EAN or other identifiers: [24a]
    public decimal? EstimatedPrice { get; set; } // Price: [20c]
    public int? PageCount { get; set; } // Number of pages: [300a]
    public string? PhysicalDetails { get; set; } // Physical description: [300b]
    public string? Dimensions { get; set; } // Dimensions: [300c]
    public string? AccompanyingMaterial { get; set; } // Accompanying materials: [300e]
    public string? Genres { get; set; } // Index Term - Genre/Form (R) [655a]
    public string? GeneralNote { get; set; } // General note: [500a]
    public string? BibliographicalNote { get; set; } // Bibliographical note: [504a]
    public string? TopicalTerms { get; set; } // Subject Added Entry - Topical Term (R) [650a] - Combined with ','
    public string? AdditionalAuthors { get; set; } // Personal author names [700a]/[700a + 700e] - Combined with ','
    
    public int CategoryId { get; set; }
    public int? ShelfId { get; set; }
    public LibraryItemStatus Status { get; set; }
    
    // Item reviewed rate
    public double AvgReviewedRate { get; set; }
    
    // References
    public CategoryDto Category { get; set; } = null!;
    public LibraryShelfDto? Shelf { get; set; }
    public LibraryItemInventoryDto? LibraryItemInventory { get; set; }
    
    // Navigations
    public List<AuthorDto> Authors { get; set; } = new();
    public List<LibraryItemInstanceDto> LibraryItemInstances { get; set; } = new();
}

public static class HomePageItemDtoExtensions
{
    public static HomePageItemDto ToHomePageItemDto(this LibraryItemDto dto)
        => new()
        {
            LibraryItemId = dto.LibraryItemId,
            Title = dto.Title,
            SubTitle = dto.SubTitle,
            Responsibility = dto.Responsibility,
            Edition = dto.Edition,
            EditionNumber = dto.EditionNumber,
            Language = dto.Language,
            OriginLanguage = dto.OriginLanguage,
            Summary = dto.Summary,
            CoverImage = dto.CoverImage,
            PublicationYear = dto.PublicationYear,
            Publisher = dto.Publisher,
            PublicationPlace = dto.PublicationPlace,
            ClassificationNumber = dto.ClassificationNumber ?? string.Empty,
            CutterNumber = dto.CutterNumber ?? string.Empty,
            Isbn = dto.Isbn,
            Ean = dto.Ean,
            EstimatedPrice = dto.EstimatedPrice,
            PageCount = dto.PageCount,
            PhysicalDetails = dto.PhysicalDetails,
            Dimensions = dto.Dimensions,
            AccompanyingMaterial = dto.AccompanyingMaterial,
            Genres = dto.Genres,
            GeneralNote = dto.GeneralNote,
            BibliographicalNote = dto.BibliographicalNote,
            TopicalTerms = dto.TopicalTerms,
            AdditionalAuthors = dto.AdditionalAuthors,
            CategoryId = dto.CategoryId,
            ShelfId = dto.ShelfId,
            Status = dto.Status,
            
            // References
            Category = dto.Category,
            Shelf = dto.Shelf,
            LibraryItemInventory = dto.LibraryItemInventory,

            // Navigations
            LibraryItemInstances = dto.LibraryItemInstances.Any() ? dto.LibraryItemInstances.ToList() : new(),
            AvgReviewedRate = dto.LibraryItemReviews.Any() ? Math.Round(dto.LibraryItemReviews.Average(lir => lir.RatingValue) * 2, MidpointRounding.AwayFromZero) / 2 : new(),
            Authors = dto.LibraryItemAuthors.Any() ? dto.LibraryItemAuthors.Select(ba => ba.Author).ToList() : new()
        };
}
