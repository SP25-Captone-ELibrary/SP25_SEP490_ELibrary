using FPTU_ELibrary.Application.Dtos.Authors;
using FPTU_ELibrary.Application.Dtos.Borrows;
using FPTU_ELibrary.Application.Dtos.Locations;
using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Application.Dtos.LibraryItems;

public class LibraryItemDetailDto
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
    public string? ClassificationNumber { get; set; } = null!; // Classification number: [82a]
    public string? CutterNumber { get; set; } = null!; // Cutter number: [82b]
    public string? Isbn { get; set; } // ISBN: [20a]
    public string? Ean { get; set; } //  EAN or other identifiers: [24a]
    public decimal? EstimatedPrice { get; set; } // Price: [20c]
    public int PageCount { get; set; } // Number of pages: [300a]
    public string? PhysicalDetails { get; set; } // Physical description: [300b]
    public string Dimensions { get; set; } = null!; // Dimensions: [300c]
    public string? AccompanyingMaterial { get; set; } // Accompanying materials: [300e]
    public string? Genres { get; set; } // Index Term - Genre/Form (R) [655a]
    public string? GeneralNote { get; set; } // General note: [500a]
    public string? BibliographicalNote { get; set; } // Bibliographical note: [504a]
    public string? TopicalTerms { get; set; } // Subject Added Entry - Topical Term (R) [650a] - Combined with ','
    public string? AdditionalAuthors { get; set; } // Personal author names [700a]/[700a + 700e] - Combined with ','
    
    // In-library management fields
    public int CategoryId { get; set; }
    public int? ShelfId { get; set; }
    public int? GroupId { get; set; }
    public LibraryItemStatus Status { get; set; }
    public bool IsDeleted { get; set; }
    public bool CanBorrow { get; set; }
    public bool IsTrained { get; set; }
    
    // Creation, update datetime and employee is charge of 
    public DateTime? TrainedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = null!;
    public string? UpdatedBy { get; set; }
    
    // Item reviewed rate
    public double AvgReviewedRate { get; set; }
    
    // Category
    public CategoryDto Category { get; set; } = null!;
    // Shelf
    public LibraryShelfDto? Shelf { get; set; }
    // Group
    public LibraryItemGroupDto? LibraryItemGroup { get; set; }
    // Inventory
    public LibraryItemInventoryDto? LibraryItemInventory { get; set; }
    // Resources
    public List<LibraryResourceDto> Resources { get; set; } = new();
    // Authors
    public List<AuthorDto> Authors { get; set; } = new();
    // Instances
    public List<LibraryItemInstanceDto> LibraryItemInstances { get; set; } = new();
    // Digital borrows
    public List<DigitalBorrowDto> DigitalBorrows { get; set; } = new();
    // Reviews
    public List<LibraryItemReviewDto> LibraryItemReviews { get; set; } = new();
}

public static class LibraryItemDetailDtoExtensions
{
    public static LibraryItemDetailDto ToLibraryItemDetailDto(this LibraryItemDto dto, List<DigitalBorrowDto>? digitalBorrows = null)
    {
        return new LibraryItemDetailDto()
        {
            // Library item details
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
            ClassificationNumber = dto.ClassificationNumber,
            CutterNumber = dto.CutterNumber,
            Isbn = dto.Isbn,
            Ean = dto.Ean,
            EstimatedPrice = dto.EstimatedPrice,
            PageCount = dto.PageCount ?? 0,
            PhysicalDetails = dto.PhysicalDetails,
            Dimensions = dto.Dimensions ?? string.Empty,
            AccompanyingMaterial = dto.AccompanyingMaterial,
            Genres = dto.Genres,
            GeneralNote = dto.GeneralNote,
            BibliographicalNote = dto.BibliographicalNote,
            TopicalTerms = dto.TopicalTerms,
            AdditionalAuthors = dto.AdditionalAuthors,
            IsDeleted = dto.IsDeleted,
            CanBorrow = dto.CanBorrow,
            IsTrained = dto.IsTrained,
            TrainedAt = dto.TrainedAt,
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt,
            CreatedBy = dto.CreatedBy,
            UpdatedBy = dto.UpdatedBy,
            // Category
            CategoryId = dto.CategoryId,
            Category = dto.Category,
            // Shelf 
            ShelfId = dto.ShelfId,
            Shelf = dto.Shelf,
            // Group 
            GroupId = dto.GroupId,
            LibraryItemGroup = dto.LibraryItemGroup,
            // Status
            Status = dto.Status,
            // Inventory 
            LibraryItemInventory = dto.LibraryItemInventory,
            // Authors
            Authors = dto.LibraryItemAuthors.Any() ? dto.LibraryItemAuthors.Select(lia => lia.Author).ToList() : new(),
            // Resources
            Resources = dto.LibraryItemResources.Any() ? dto.LibraryItemResources.Select(lir => lir.LibraryResource).ToList() : new(),
            // Item instances
            LibraryItemInstances = dto.LibraryItemInstances.Any() ? dto.LibraryItemInstances.ToList() : new(),
            // Average item reviews
            AvgReviewedRate = dto.LibraryItemReviews.Any() ? Math.Round(dto.LibraryItemReviews.Average(lir => lir.RatingValue) * 2, MidpointRounding.AwayFromZero) / 2 : 0,
            // Library item reviews
            LibraryItemReviews = dto.LibraryItemReviews.Any() ? dto.LibraryItemReviews.ToList() : new(),
            // Digital borrows
            DigitalBorrows = digitalBorrows ?? new()
        };
    }

    public static LibraryItemDetailDto ToLibraryItemDetailWithoutGroupDto(this LibraryItemDto dto,
        List<DigitalBorrowDto>? digitalBorrows = null)
    {
        return new LibraryItemDetailDto()
        {
            // Library item details
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
            ClassificationNumber = dto.ClassificationNumber,
            CutterNumber = dto.CutterNumber,
            Isbn = dto.Isbn,
            Ean = dto.Ean,
            EstimatedPrice = dto.EstimatedPrice,
            PageCount = dto.PageCount ?? 0,
            PhysicalDetails = dto.PhysicalDetails,
            Dimensions = dto.Dimensions ?? string.Empty,
            AccompanyingMaterial = dto.AccompanyingMaterial,
            Genres = dto.Genres,
            GeneralNote = dto.GeneralNote,
            BibliographicalNote = dto.BibliographicalNote,
            TopicalTerms = dto.TopicalTerms,
            AdditionalAuthors = dto.AdditionalAuthors,
            IsDeleted = dto.IsDeleted,
            CanBorrow = dto.CanBorrow,
            IsTrained = dto.IsTrained,
            TrainedAt = dto.TrainedAt,
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt,
            CreatedBy = dto.CreatedBy,
            UpdatedBy = dto.UpdatedBy,
            // Category
            CategoryId = dto.CategoryId,
            Category = dto.Category,
            // Shelf 
            ShelfId = dto.ShelfId,
            Shelf = dto.Shelf,
            // Group 
            GroupId = dto.GroupId,
            // Status
            Status = dto.Status,
            // Inventory 
            LibraryItemInventory = dto.LibraryItemInventory,
            // Authors
            Authors = dto.LibraryItemAuthors.Any() ? dto.LibraryItemAuthors.Select(lia => lia.Author).ToList() : new(),
            // Resources
            Resources = dto.LibraryItemResources.Any() ? dto.LibraryItemResources.Select(lir => lir.LibraryResource).ToList() : new(),
            // Item instances
            LibraryItemInstances = dto.LibraryItemInstances.Any() ? dto.LibraryItemInstances.ToList() : new(),
            // Average item reviews
            AvgReviewedRate = dto.LibraryItemReviews.Any() ? Math.Round(dto.LibraryItemReviews.Average(lir => lir.RatingValue) * 2, MidpointRounding.AwayFromZero) / 2 : 0,
            // Library item reviews
            LibraryItemReviews = dto.LibraryItemReviews.Any() ? dto.LibraryItemReviews.ToList() : new(),
            // Digital borrows
            DigitalBorrows = digitalBorrows ?? new()
        };
    }

    public static LibraryItemDetailDto ToLibraryItemGroupedDetailDto(this LibraryItemDto dto, 
        List<DigitalBorrowDto>? digitalBorrows = null)
    {
        return new LibraryItemDetailDto()
        {
            // Library item details
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
            ClassificationNumber = dto.ClassificationNumber,
            CutterNumber = dto.CutterNumber,
            Isbn = dto.Isbn,
            Ean = dto.Ean,
            EstimatedPrice = dto.EstimatedPrice,
            PageCount = dto.PageCount ?? 0,
            PhysicalDetails = dto.PhysicalDetails,
            Dimensions = dto.Dimensions ?? string.Empty,
            AccompanyingMaterial = dto.AccompanyingMaterial,
            Genres = dto.Genres,
            GeneralNote = dto.GeneralNote,
            BibliographicalNote = dto.BibliographicalNote,
            TopicalTerms = dto.TopicalTerms,
            AdditionalAuthors = dto.AdditionalAuthors,
            IsDeleted = dto.IsDeleted,
            CanBorrow = dto.CanBorrow,
            IsTrained = dto.IsTrained,
            TrainedAt = dto.TrainedAt,
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt,
            CreatedBy = dto.CreatedBy,
            UpdatedBy = dto.UpdatedBy,
            // Category
            CategoryId = dto.CategoryId,
            Category = dto.Category,
            // Shelf 
            ShelfId = dto.ShelfId,
            Shelf = dto.Shelf,
            // Group 
            GroupId = dto.GroupId,
            LibraryItemGroup = dto.LibraryItemGroup,
            // Status
            Status = dto.Status,
            // Inventory 
            LibraryItemInventory = dto.LibraryItemInventory,
            // Authors
            Authors = dto.LibraryItemAuthors.Any() ? dto.LibraryItemAuthors.Select(lia => lia.Author).ToList() : new (),
            // Resources
            Resources = new (),
            // Item instances
            LibraryItemInstances = new(),
            // Average item reviews
            AvgReviewedRate = dto.LibraryItemReviews.Any() ? Math.Round(dto.LibraryItemReviews.Average(lir => lir.RatingValue) * 2, MidpointRounding.AwayFromZero) / 2 : 0,
            // Library item reviews
            LibraryItemReviews = dto.LibraryItemReviews.Any() ? dto.LibraryItemReviews.ToList() : new(),
            // Digital borrows
            DigitalBorrows = digitalBorrows ?? new()
        };
    }
}