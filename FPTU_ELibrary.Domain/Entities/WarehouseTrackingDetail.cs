using System.Reflection.Metadata.Ecma335;
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
    
    // With specific warehouse tracking 
    public int TrackingId { get; set; }
    
    // Specific item ID (NULL initially during Goods Receipt)
    public int? LibraryItemId { get; set; }
    
    // For specific item category
    public int CategoryId { get; set; }
    
    // Condition
    public int ConditionId { get; set; }
    
    // Stock transaction type
    public StockTransactionType StockTransactionType { get; set; }
    
    // Creation, update datetime 
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = null!;
    public string? UpdatedBy { get; set; }
    
    // Barcode range 
    public string? BarcodeRangeFrom { get; set; }
    public string? BarcodeRangeTo { get; set; }

    // Mark as is glue barcode 
    public bool HasGlueBarcode { get; set; }
        
    // Supplement reason summary
    public string? SupplementRequestReason { get; set; }
    public int? BorrowSuccessCount { get; set; }
    public int? BorrowRequestCount { get; set; }
    public int? BorrowFailedCount { get; set; }
    public int? TotalSatisfactionUnits { get; set; }
    public int? AvailableUnits { get; set; }
    public int? NeedUnits { get; set; }
    public double? AverageNeedSatisfactionRate { get; set; }
    public double? BorrowExtensionRate { get; set; }

    #region Archived Fields
    public int? ReserveCount { get; set; }
    public double? BorrowFailedRate { get; set; }
    #endregion
    
    // Navigation properties
    public LibraryItem? LibraryItem { get; set; }
    public WarehouseTracking WarehouseTracking { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public LibraryItemCondition Condition { get; set; } = null!;
}