namespace FPTU_ELibrary.API.Payloads.Requests.WarehouseTracking;

public class CreateSupplementDetailRequest
{
    public string Title { get; set; } = null!;
    public string Author { get; set; } = null!;
    public string Publisher { get; set; } = null!;
    public string? PublishedDate { get; set; }
    public string? Description { get; set; }
    public string? Isbn { get; set; }
    public int PageCount { get; set; }
    public decimal EstimatedPrice { get; set; } 
    public string? Dimensions { get; set; }
    public string? Categories { get; set; }
    public string? Language { get; set; }
    public int? AverageRating { get; set; }
    public int? RatingsCount { get; set; }
    public string? CoverImageLink { get; set; } // Thumbnail
    public string? PreviewLink { get; set; }
    public string? InfoLink { get; set; }
    
    // Related with which item
    public int RelatedLibraryItemId { get; set; }
}