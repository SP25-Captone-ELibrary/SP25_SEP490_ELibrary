namespace FPTU_ELibrary.API.Payloads.Requests.WarehouseTracking;

public class CreateSupplementRequest
{
    public int TotalItem { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime EntryDate { get; set; }
    public DateTime DataFinalizationDate { get; set; }
    public string? Description { get; set; }
    public List<CreateSupplementItemDetailRequest> WarehouseTrackingDetails { get; set; } = new();
    public List<CreateSupplementDetailRequest> SupplementRequestDetails { get; set; } = new();
}