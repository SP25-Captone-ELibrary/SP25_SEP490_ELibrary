using CsvHelper.Configuration.Attributes;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Application.Utils;
using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Application.Dtos.WarehouseTrackings;

public class WarehouseTrackingDetailCsvRecord
{
    // Item Name
    [Name("Tên sách")]
    public string ItemName { get; set; } = null!;
    
    // Number of items
    [Name("Số lượng")]
    public int ItemTotal { get; set; }

    // ISBN
    [Name("ISBN")]
    public string? Isbn { get; set; }
    
    // Unit price of the item
    [Name("Giá tiền")]
    public decimal UnitPrice { get; set; }
    
    // Total amount for the detail line
    [Name("Thành tiền")]
    public decimal TotalAmount { get; set; }
    
    // Reason for stock-out or adjustment
    [Name("Nguyên nhân")]
    public string? Reason { get; set; }

    // For specific item category
    [Name("Phân loại")] 
    public string Category { get; set; } = null!;
    
    // Item condition
    [Name("Tình trạng")] 
    public string Condition { get; set; } = null!;
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