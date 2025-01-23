namespace FPTU_ELibrary.API.Payloads.Requests.LibraryItem;

public class UpdateLibraryResourceRequest
{
    public string ResourceTitle { get; set; } = null!;
    
    public string ResourceUrl { get; set; } = null!;
    public decimal? ResourceSize { get; set; }
    public string Provider { get; set; } = null!;
    public string FileFormat { get; set; } = null!;
    
    // Read-only, this field to ensure that not update ProviderPublicId
    public string ProviderPublicId { get; set; } = null!;
    public int DefaultBorrowDurationDays { get; set; }
    public decimal BorrowPrice { get; set; }
}