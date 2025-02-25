using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.API.Payloads.Requests.WarehouseTracking;

public class CreateWarehouseTrackingRequest
{
    public int SupplierId { get; set; }
    public int TotalItem { get; set; }
    public decimal TotalAmount { get; set; }
    public TrackingType TrackingType { get; set; }
    public string? TransferLocation { get; set; }
    public string? Description { get; set; }
    public DateTime EntryDate { get; set; }
    public DateTime? ExpectedReturnDate { get; set; }
    
    // Combine with importing warehouse tracking details while create new (if any)
    public IFormFile? File { get; set; }
    public List<IFormFile> CoverImageFiles { get; set; } = new();
    public string[]? ScanningFields { get; set; }
    public DuplicateHandle? DuplicateHandle { get; set; }
}