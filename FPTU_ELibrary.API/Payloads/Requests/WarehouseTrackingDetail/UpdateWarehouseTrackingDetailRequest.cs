using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.API.Payloads.Requests.WarehouseTrackingDetail;

public class UpdateWarehouseTrackingDetailRequest
{
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
    
    // For specific item category
    public int CategoryId { get; set; }

    // Item condition
    public int ConditionId { get; set; }
    
    // Stock transaction type
    public StockTransactionType StockTransactionType { get; set; }
}