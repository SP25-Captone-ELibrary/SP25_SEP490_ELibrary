using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Interfaces;

namespace FPTU_ELibrary.Domain.Entities;

public class WarehouseTrackingDetail : IAuditableEntity
{
    public int TrackingDetailId { get; set; }

    // Item Name
    public string ItemName { get; set; } = null!;
    
    // Number of items
    public int ItemTotal { get; set; }

    // ISBN
    public string? Isbn { get; set; }
    
    // Unit price of the item
    public decimal UnitPrice { get; set; }
    
    // Total amount for the detail line
    public decimal TotalAmount { get; set; }
    
    // Reason for stock-out or adjustment
    public TrackingDetailReason? Reason { get; set; }

    // With specific warehouse tracking 
    public int TrackingId { get; set; }
    
    // Specific item ID (NULL initially during Goods Receipt)
    public int? LibraryItemId { get; set; }
    
    // For specific item category
    public int CategoryId { get; set; }
    
    // Condition
    public int ConditionId { get; set; }
    
    // Creation, update datetime 
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = null!;
    public string? UpdatedBy { get; set; }
        
    // Navigation properties
    public LibraryItem? LibraryItem { get; set; }
    public WarehouseTracking WarehouseTracking { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public LibraryItemCondition Condition { get; set; } = null!;
}