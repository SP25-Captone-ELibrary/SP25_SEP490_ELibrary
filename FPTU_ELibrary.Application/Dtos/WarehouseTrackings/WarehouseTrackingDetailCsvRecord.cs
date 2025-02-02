using CsvHelper.Configuration.Attributes;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Application.Utils;
using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Application.Dtos.WarehouseTrackings;

public class WarehouseTrackingDetailCsvRecord
{
    // Item Name
    [Name("ItemName")]
    public string ItemName { get; set; } = null!;
    
    // Number of items
    [Name("ItemTotal")]
    public int ItemTotal { get; set; }

    // ISBN
    [Name("ISBN")]
    public string? Isbn { get; set; }
    
    // Unit price of the item
    [Name("UnitPrice")]
    public decimal UnitPrice { get; set; }
    
    // Total amount for the detail line
    [Name("TotalAmount")]
    public decimal TotalAmount { get; set; }
    
    // Reason for stock-out or adjustment
    [Name("Reason")]
    public string? Reason { get; set; }

    // For specific item category
    [Name("Category")] 
    public string Category { get; set; } = null!;
}

public static class WarehouseTrackingDetailCsvRecordExtensions
{
    public static WarehouseTrackingDetailDto ToWarehouseTrackingDetailDto(this WarehouseTrackingDetailCsvRecord record)
    {
        return new()
        {
            ItemName = record.ItemName,
            ItemTotal = record.ItemTotal,
            Isbn = ISBN.CleanIsbn(record.Isbn ?? string.Empty),
            UnitPrice = record.UnitPrice,
            TotalAmount = record.TotalAmount,
            Reason = !string.IsNullOrEmpty(record.Reason) 
                ? Enum.TryParse(record.Reason, out TrackingDetailReason trackingReason) 
                    ? trackingReason : (TrackingDetailReason?)null
                : null
        };
    }
}