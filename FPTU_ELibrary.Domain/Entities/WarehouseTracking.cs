using System.Text.Json.Serialization;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Interfaces;

namespace FPTU_ELibrary.Domain.Entities;

public class WarehouseTracking : IAuditableEntity
{
    public int TrackingId { get; set; }
    public int? SupplierId { get; set; }
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
    // Data finalization date
    public DateTime? DataFinalizationDate { get; set; }
    
    // Creation, update datetime 
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = null!;
    public string? UpdatedBy { get; set; }

    // Finalized stock-in file 
    public string? FinalizedFile { get; set; }
    
    // References
    public Supplier? Supplier { get; set; }
    public WarehouseTrackingInventory WarehouseTrackingInventory { get; set; } = null!;
    
    [JsonIgnore]
    public ICollection<WarehouseTrackingDetail> WarehouseTrackingDetails { get; set; } = new List<WarehouseTrackingDetail>();
    
    public ICollection<SupplementRequestDetail> SupplementRequestDetails { get; set; } = new List<SupplementRequestDetail>();
}