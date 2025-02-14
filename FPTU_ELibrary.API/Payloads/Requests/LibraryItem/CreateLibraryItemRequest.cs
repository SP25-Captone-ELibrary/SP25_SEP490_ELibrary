using FPTU_ELibrary.API.Payloads.Requests.LibraryItemInstance;

namespace FPTU_ELibrary.API.Payloads.Requests.LibraryItem;

public class CreateLibraryItemRequest
{
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
    public int? GroupId { get; set; }
    
    // Warehouse tracking detail
    public int TrackingDetailId { get; set; }
    
    // Instances  
    public List<CreateItemInstanceRequest>? LibraryItemInstances { get; set; } = new();
    // Book authors
    public List<int> AuthorIds { get; set; } = new();
    // Book resources
    public List<CreateLibraryResourceRequest>? LibraryResources { get; set; } = new();
}
