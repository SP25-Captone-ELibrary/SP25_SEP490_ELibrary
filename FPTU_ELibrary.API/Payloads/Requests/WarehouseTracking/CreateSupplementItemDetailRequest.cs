namespace FPTU_ELibrary.API.Payloads.Requests.WarehouseTracking;

public class CreateSupplementItemDetailRequest
{
    // For specific item
    public int LibraryItemId { get; set; }
 
    // With specific category
    public int CategoryId { get; set; }
    
    // With specific condition id
    public int ConditionId { get; set; }
    
    // Supplement item details 
    public string ItemName { get; set; } = null!;
    public int ItemTotal { get; set; }
    public string Isbn { get; set; } = null!;
    public decimal UnitPrice { get; set; }
    public decimal TotalAmount { get; set; }
    
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
    // public int? ReserveCount { get; set; }
    // public double? BorrowFailedRate { get; set; }
    #endregion
}