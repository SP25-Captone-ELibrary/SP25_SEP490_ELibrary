using System.Text.Json.Serialization;
using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Domain.Entities;

public class WarehouseTracking
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
    public DateTime? TrainedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = null!;
    public string? UpdatedBy { get; set; }
    
    // Mapping entities
    public Supplier Supplier { get; set; } = null!;

    [JsonIgnore]
    public ICollection<WarehouseTrackingDetail> WarehouseTrackingDetails { get; set; } =
        new List<WarehouseTrackingDetail>();
}