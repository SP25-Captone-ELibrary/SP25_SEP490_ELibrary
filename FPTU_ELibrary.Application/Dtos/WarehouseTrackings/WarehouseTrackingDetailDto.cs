using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Application.Dtos.Suppliers;
using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Application.Dtos.WarehouseTrackings;

public class WarehouseTrackingDetailDto
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
    public int? ReserveCount { get; set; }
    public int? BorrowFailedCount { get; set; }
    public double? BorrowFailedRate { get; set; }
    public int? AvailableUnits { get; set; }
    public int? NeedUnits { get; set; }
    public double? AverageNeedSatisfactionRate { get; set; }
    
    // Navigation properties
    public LibraryItemDto? LibraryItem { get; set; }
    public WarehouseTrackingDto WarehouseTracking { get; set; } = null!;
    public CategoryDto Category { get; set; } = null!;
    public LibraryItemConditionDto Condition { get; set; } = null!;
}