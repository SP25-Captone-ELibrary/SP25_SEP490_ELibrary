using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.API.Payloads.Requests.WarehouseTracking;

public class UpdateWarehouseTrackingRequest
{
    public int SupplierId { get; set; }
    public int TotalItem { get; set; }
    public decimal TotalAmount { get; set; }
    public TrackingType TrackingType { get; set; }
    public string? TransferLocation { get; set; }
    public string? Description { get; set; }
    public DateTime EntryDate { get; set; }
    public DateTime? ExpectedReturnDate { get; set; }
}