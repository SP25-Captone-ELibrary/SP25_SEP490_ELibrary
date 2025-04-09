using System.Text.Json.Serialization;

namespace FPTU_ELibrary.Domain.Entities;

public class SupplementRequestDetail
{
    // Key
    public int SupplementRequestDetailId { get; set; }
    
    // Supplement request details
    public string Title { get; set; } = null!;
    public string Author { get; set; } = null!;
    public string Publisher { get; set; } = null!;
    public string? PublishedDate { get; set; }
    public string? Description { get; set; }
    public string? Isbn { get; set; }
    public int PageCount { get; set; }
    public string? Dimensions { get; set; }
    public decimal? EstimatedPrice { get; set; } 
    public string? Categories { get; set; }
    public int? AverageRating { get; set; }
    public int? RatingsCount { get; set; }
    public string? Language { get; set; }
    public string? CoverImageLink { get; set; } // Thumbnail
    public string? PreviewLink { get; set; }
    public string? InfoLink { get; set; }
    
    // Description
    public string? SupplementRequestReason { get; set; }
    
    // Related with which item
    public int RelatedLibraryItemId { get; set; }
    
    // For specific tracking 
    public int TrackingId { get; set; }
    
    // Reference
    public LibraryItem RelatedLibraryItem { get; set; } = null!;
    
    [JsonIgnore]
    public WarehouseTracking WarehouseTracking { get; set; } = null!;
}