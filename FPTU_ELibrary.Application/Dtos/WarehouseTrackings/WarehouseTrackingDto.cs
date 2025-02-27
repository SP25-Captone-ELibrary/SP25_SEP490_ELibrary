using System.Text.Json.Serialization;
using FPTU_ELibrary.Application.Dtos.Suppliers;
using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Application.Dtos.WarehouseTrackings;

public class WarehouseTrackingDto
{
    public int TrackingId { get; set; }
    public int SupplierId { get; set; }
    public string ReceiptNumber { get; set; } = null!;
    public int TotalItem { get; set; }
    public decimal TotalAmount { get; set; }
    public TrackingType TrackingType { get; set; }
    public string? TransferLocation { get; set; }
    public string? Description { get; set; }
    public WarehouseTrackingStatus Status { get; set; }
    
    // Expected return date (use for temporary transfers or lending stock between locations)
    public DateTime? ExpectedReturnDate { get; set; }
    // Actual return date (for completed returns)
    public DateTime? ActualReturnDate { get; set; }
    // Entry date    
    public DateTime EntryDate { get; set; }
    
    // Creation, update datetime and employee is charge of 
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = null!;
    public string? UpdatedBy { get; set; }

    // References
    public SupplierDto Supplier { get; set; } = null!;
    public WarehouseTrackingInventoryDto WarehouseTrackingInventory { get; set; } = null!;
    
    [JsonIgnore]
    public ICollection<WarehouseTrackingDetailDto> WarehouseTrackingDetails { get; set; } =
        new List<WarehouseTrackingDetailDto>();
}