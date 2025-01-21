namespace FPTU_ELibrary.API.Payloads.Requests.LibraryItem;

public class CreateLibraryResourceRequest
{
    public string ResourceTitle { get; set; } = null!;
    public string ResourceType { get; set; } = null!;
    public string Provider { get; set; } = null!;
    public string ResourceUrl { get; set; } = null!;
    public string ProviderPublicId { get; set; } = null!;
    public string FileFormat { get; set; } = null!;
    public decimal? ResourceSize { get; set; }
    public int DefaultBorrowDurationDays { get; set; }
    public decimal BorrowPrice { get; set; }
}