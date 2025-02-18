using FPTU_ELibrary.Application.Dtos.LibraryItems;

namespace FPTU_ELibrary.Application.Dtos.WarehouseTrackings;

public class WarehouseTrackingCategorySummaryDto
{
    public CategoryDto Category { get; set; } = null!;
    public int TotalItem { get; set; }
    public int TotalInstanceItem { get; set; }
    public int TotalCatalogedItem { get; set; }
    public int TotalCatalogedInstanceItem { get; set; }
    public decimal TotalPrice { get; set; }
}

public static class WarehouseTrackingCategorySummaryDtoExtensions
{
    public static WarehouseTrackingCategorySummaryDto ToSummaryDto(
        this List<WarehouseTrackingDetailDto> groupedDetail,
        CategoryDto category)
    {
        // Total item 
        var totalItem = groupedDetail.Count;
        // Total cataloged item 
        var totalCatalogedItem = groupedDetail.Count(g => g.LibraryItemId != null);
        // Total instance item 
        var totalInstanceItem = groupedDetail.Select(g => g.ItemTotal).Sum();
        // Total actual instance item 
        var totalActualInstanceItem = groupedDetail
            .Where(g => g.LibraryItem != null && g.LibraryItem.LibraryItemInventory != null)
            .Select(g => g.LibraryItem?.LibraryItemInventory?.TotalUnits)
            .Sum();
        // Total price
        var totalPrice = groupedDetail.Select(g => g.TotalAmount).Sum();
            
        return new()
        {
            Category = category,
            TotalItem = totalItem,
            TotalCatalogedItem = totalCatalogedItem,
            TotalInstanceItem = totalInstanceItem,
            TotalCatalogedInstanceItem = totalActualInstanceItem ?? 0,
            TotalPrice = totalPrice
        };
    }
}