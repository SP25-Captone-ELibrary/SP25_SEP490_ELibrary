using System.Text.Json.Serialization;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Interfaces;

namespace FPTU_ELibrary.Domain.Entities;

public class LibraryItem : IAuditableEntity
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
    public string? Dimensions { get; set; } // Dimensions: [300c]
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
    public Category Category { get; set; } = null!;
    public LibraryShelf? Shelf { get; set; }
    public LibraryItemGroup? LibraryItemGroup { get; set; } 
    public LibraryItemInventory? LibraryItemInventory { get; set; }
    public ICollection<LibraryItemInstance> LibraryItemInstances { get; set; } = new List<LibraryItemInstance>();
    public ICollection<LibraryItemAuthor> LibraryItemAuthors { get; set; } = new List<LibraryItemAuthor>();
    public ICollection<LibraryItemResource> LibraryItemResources { get; set; } = new List<LibraryItemResource>();
    public ICollection<LibraryItemReview> LibraryItemReviews { get; set; } = new List<LibraryItemReview>();
    
    [JsonIgnore]
    public ICollection<UserFavorite> UserFavorites { get; set; } = new List<UserFavorite>();
    
    [JsonIgnore]
    public ICollection<BorrowRequestDetail> BorrowRequestDetails { get; set; } = new List<BorrowRequestDetail>();
    
    [JsonIgnore]
    public ICollection<ReservationQueue> ReservationQueues { get; set; } = new List<ReservationQueue>(); 
    
    [JsonIgnore]
    public ICollection<WarehouseTrackingDetail> WarehouseTrackingDetails { get; set; } = new List<WarehouseTrackingDetail>();
    
    [JsonIgnore]
    public ICollection<AITrainingDetail> TrainingDetails { get; set; } = new List<AITrainingDetail>();
}