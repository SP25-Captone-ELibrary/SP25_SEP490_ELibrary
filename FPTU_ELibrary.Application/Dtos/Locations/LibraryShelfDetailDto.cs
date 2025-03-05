using System.Text.Json.Serialization;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Application.Dtos.Locations;

public class LibraryShelfDetailDto
{
    // Key
    public int ShelfId { get; set; }
    
    // Section belongs to 
    public int SectionId { get; set; }
    
    // Shelf detail
    public string ShelfNumber { get; set; } = null!;
    public string? EngShelfName { get; set; }
    public string? VieShelfName { get; set; }
    
    // DDC Range
    public decimal ClassificationNumberRangeFrom { get; set; }
    public decimal ClassificationNumberRangeTo { get; set; }

    // Creation and update datetime
    public DateTime CreateDate { get; set; }
    public DateTime? UpdateDate { get; set; }

    // Mark as delete or not 
    public bool IsDeleted { get; set; }
    
    // Unit summary
    public LibraryShelfDetailUnitSummaryDto UnitSummary { get; set; } = null!;
    
    public List<LibraryShelfDetailItemDto> LibraryItems { get; set; } = new ();
}

public class LibraryShelfDetailItemDto
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
    public string? ClassificationNumber { get; set; } // Classification number: [82a]
    public string? CutterNumber { get; set; } // Cutter number: [82b]
    public string? Isbn { get; set; } // ISBN: [20a]
    public string? Ean { get; set; } //  EAN or other identifiers: [24a]
    public decimal? EstimatedPrice { get; set; } // Price: [20c]
    public int? PageCount { get; set; } // Number of pages: [300a]
    public string? PhysicalDetails { get; set; } // Physical description: [300b]
    public string? Dimensions { get; set; } = null!; // Dimensions: [300c]
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
    
    // Mapping entities
    public CategoryDto Category { get; set; } = null!;
    public LibraryItemInventoryDto? LibraryItemInventory { get; set; }
    public List<LibraryItemAuthorDto> LibraryItemAuthors { get; set; } = new();
    public List<LibraryItemInstanceDto> LibraryItemInstances { get; set; } = new();
    public List<LibraryItemReviewDto> LibraryItemReviews { get; set; } = new();
}

public class LibraryShelfDetailUnitSummaryDto
{
    // Add summary fields
    public int TotalUnits { get; set; }
    public int TotalAvailableUnits { get; set; }
    public int TotalRequestUnits { get; set; }
    public int TotalBorrowedUnits { get; set; }
    public int TotalReservedUnits { get; set; }

    // Status summary
    public int TotalOverdueUnits { get; set; }
    public int TotalCanBorrow { get; set; }
    
    // Condition summary
    public int TotalDamagedUnits { get; set; }
    public int TotalLostUnits { get; set; }
    
    // Digital count
    public int TotalDigitalResources { get; set; }
}

public static class LibraryShelfDetailDtoExtensions
{
    public static LibraryShelfDetailDto ToShelfDetailDto(this LibraryShelfDto dto)
    {
        // Aggregate total units
        var totalUnits = dto.LibraryItems.Select(li => li.LibraryItemInventory?.TotalUnits ?? 0).Sum();
        // Aggregate total available units
        var totalAvailableUnits = dto.LibraryItems.Select(li => li.LibraryItemInventory?.AvailableUnits ?? 0).Sum();
        // Aggregate total request units
        var totalRequestUnits = dto.LibraryItems.Select(li => li.LibraryItemInventory?.RequestUnits ?? 0).Sum();
        // Aggregate total borrowed units
        var totalBorrowedUnits = dto.LibraryItems.Select(li => li.LibraryItemInventory?.BorrowedUnits ?? 0).Sum();
        // Aggregate total reserved units
        var totalReservedUnits = dto.LibraryItems.Select(li => li.LibraryItemInventory?.ReservedUnits ?? 0).Sum();
        
        // Total overdue units
        var borrowRecords = dto.LibraryItems
            .SelectMany(li => li.LibraryItemInstances)
            .Select(lii => lii.BorrowRecordDetails.Select(rd => rd.BorrowRecord).FirstOrDefault());
        var totalOverdueUnits = borrowRecords
            .Where(br => br != null)
            .Count(br => br?.Status == BorrowRecordStatus.Overdue);
        // Total can borrow
        var totalCanBorrow = dto.LibraryItems.Count(li => li.CanBorrow);
        // Total damaged units
        var totalDamagedUnits = dto.LibraryItems
            .SelectMany(li => li.LibraryItemInstances)
            .SelectMany(lii => lii.LibraryItemConditionHistories)
            .Count(h => Equals(h.Condition.EnglishName, nameof(LibraryItemConditionStatus.Damaged)));
        // Total lost units
        var totalLostUnits = dto.LibraryItems
            .SelectMany(li => li.LibraryItemInstances)
            .SelectMany(lii => lii.LibraryItemConditionHistories)
            .Count(h => Equals(h.Condition.EnglishName, nameof(LibraryItemConditionStatus.Lost)));
        // Total digital resources
        var totalDigitalResources = dto.LibraryItems.Sum(li => li.LibraryItemResources.Count);
        
        return new()
        {
            ShelfId = dto.ShelfId,
            SectionId = dto.SectionId,
            ShelfNumber = dto.ShelfNumber,
            EngShelfName = dto.EngShelfName,
            VieShelfName = dto.VieShelfName,
            ClassificationNumberRangeFrom = dto.ClassificationNumberRangeFrom,
            ClassificationNumberRangeTo = dto.ClassificationNumberRangeTo,
            CreateDate = dto.CreateDate,
            UpdateDate = dto.UpdateDate,
            IsDeleted = dto.IsDeleted,
            LibraryItems = dto.LibraryItems.Select(li => new LibraryShelfDetailItemDto()
            {
                LibraryItemId = li.LibraryItemId,
                Title = li.Title,
                SubTitle = li.SubTitle,
                Responsibility = li.Responsibility,
                Edition = li.Edition,
                EditionNumber = li.EditionNumber,
                Language = li.Language,
                OriginLanguage = li.OriginLanguage,
                Summary = li.Summary,
                CoverImage = li.CoverImage,
                PublicationYear = li.PublicationYear,
                Publisher = li.Publisher,
                PublicationPlace = li.PublicationPlace,
                ClassificationNumber = li.ClassificationNumber,
                CutterNumber = li.CutterNumber,
                Isbn = li.Isbn,
                Ean = li.Ean,
                EstimatedPrice = li.EstimatedPrice,
                PageCount = li.PageCount,
                PhysicalDetails = li.PhysicalDetails,
                Dimensions = li.Dimensions,
                AccompanyingMaterial = li.AccompanyingMaterial,
                Genres = li.Genres,
                GeneralNote = li.GeneralNote,
                BibliographicalNote = li.BibliographicalNote,
                TopicalTerms = li.TopicalTerms,
                AdditionalAuthors = li.AdditionalAuthors,
                CategoryId = li.CategoryId,
                ShelfId = li.ShelfId,
                GroupId = li.GroupId,
                Status = li.Status,
                IsDeleted = li.IsDeleted,
                CanBorrow = li.CanBorrow,
                IsTrained = li.IsTrained,
                TrainedAt = li.TrainedAt,
                CreatedAt = li.CreatedAt,
                UpdatedAt = li.UpdatedAt,
                CreatedBy = li.CreatedBy,
                UpdatedBy = li.UpdatedBy,
                Category = li.Category,
                LibraryItemInventory = li.LibraryItemInventory,
                LibraryItemAuthors = li.LibraryItemAuthors.Any() ? li.LibraryItemAuthors.ToList() : new(),
                LibraryItemInstances = li.LibraryItemInstances.Any() ? li.LibraryItemInstances.ToList() : new(),
                LibraryItemReviews = li.LibraryItemReviews.Any() ? li.LibraryItemReviews.ToList() : new(),
            }).ToList(),
            UnitSummary = new()
            {
                TotalUnits = totalUnits,
                TotalAvailableUnits = totalAvailableUnits,
                TotalRequestUnits = totalRequestUnits,
                TotalBorrowedUnits = totalBorrowedUnits,
                TotalReservedUnits = totalReservedUnits,
                TotalCanBorrow = totalCanBorrow,
                TotalDamagedUnits = totalDamagedUnits,
                TotalLostUnits = totalLostUnits,
                TotalOverdueUnits = totalOverdueUnits,
                TotalDigitalResources = totalDigitalResources
            }
        };
    }
}