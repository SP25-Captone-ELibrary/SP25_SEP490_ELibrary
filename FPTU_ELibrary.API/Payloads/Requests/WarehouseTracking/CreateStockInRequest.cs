namespace FPTU_ELibrary.API.Payloads.Requests.WarehouseTracking;

public class CreateStockInRequest
{
    public int SupplierId { get; set; }
    public string? Description { get; set; }
    public DateTime EntryDate { get; set; }
    public int TotalItem { get; set; }
    public decimal TotalAmount { get; set; }
    public List<CreateStockInDetailRequest> WarehouseTrackingDetails { get; set; } = new();
}