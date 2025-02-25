using CsvHelper.Configuration.Attributes;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Application.Extensions;
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
    
    // For specific item category
    [Name("Phân loại")] 
    public string Category { get; set; } = null!;
    
    // Item condition
    [Name("Tình trạng")] 
    public string Condition { get; set; } = null!;
    
    // Stock transaction type 
    [Name("Loại biến động kho")] 
    public string StockTransactionType { get; set; } = null!;
}

public static class WarehouseTrackingDetailCsvRecordExtensions
{
    public static WarehouseTrackingDetailDto ToWarehouseTrackingDetailDto(
        this WarehouseTrackingDetailCsvRecord record,
        List<CategoryDto> categories,
        List<LibraryItemConditionDto>? conditions = null)
    {
        StockTransactionType type;
        // Try to validate input stock transaction type
        if (!Enum.TryParse(record.StockTransactionType, true, out type)) // Not valid
        {
            // Try to get from description
            var getRes = EnumExtensions.GetValueFromDescription<StockTransactionType>(record.StockTransactionType);
            if (getRes != null && getRes is StockTransactionType) type = (StockTransactionType) getRes;
        }
        
        // Initialize warehouse tracking detail dto
        var dtoRes = new WarehouseTrackingDetailDto()
        {
            ItemName = record.ItemName,
            ItemTotal = record.ItemTotal,
            Isbn = ISBN.CleanIsbn(record.Isbn ?? string.Empty),
            UnitPrice = record.UnitPrice,
            TotalAmount = record.TotalAmount,
            StockTransactionType = type
        };

        // Try to retrieve condition
        var conditionId = conditions?.FirstOrDefault(c =>
            Equals(c.EnglishName.ToLower(), record.Condition.ToLower()) ||
            Equals(c.VietnameseName.ToLower(), record.Condition.ToLower())
        )?.ConditionId;
        if(conditionId != null && conditionId > 0) dtoRes.ConditionId = int.Parse(conditionId.ToString()!);
    
        // Try to retrieve category 
        var categoryId = categories.FirstOrDefault(c =>
            Equals(c.EnglishName.ToLower(), record.Category.ToLower()) || 
            Equals(c.VietnameseName.ToLower(), record.Category.ToLower()))?.CategoryId;
        if(categoryId != null && categoryId > 0) dtoRes.CategoryId = int.Parse(categoryId.ToString()!);
        
        return dtoRes;
    }
}