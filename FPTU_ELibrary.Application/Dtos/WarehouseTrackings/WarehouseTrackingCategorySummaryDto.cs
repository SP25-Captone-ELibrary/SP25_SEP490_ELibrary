using FPTU_ELibrary.Application.Dtos.LibraryItems;

namespace FPTU_ELibrary.Application.Dtos.WarehouseTrackings;

public class WarehouseTrackingCategorySummaryDto
{
    public CategoryDto Category { get; set; } = null!;
    public int? TotalItem { get; set; }
    public int? TotalInstanceItem { get; set; }
    public int? TotalCatalogedItem { get; set; }
    public int? TotalCatalogedInstanceItem { get; set; }
    public decimal? TotalPrice { get; set; }
}

public static class WarehouseTrackingCategorySummaryDtoExtensions
{
    public static WarehouseTrackingCategorySummaryDto ToSummaryDto(
        this List<WarehouseTrackingDetailDto> groupedDetail,
        CategoryDto category)
    {
        // Total item <- Number of warehouse tracking details  
        var totalItem = groupedDetail.Count;
        // Total instance item <- Sum of each warehouse tracking detail's item total
        var totalInstanceItem = groupedDetail.Count != 0
            ? groupedDetail.Select(wtd => wtd.ItemTotal).Sum() 
            : 0;
        // Total cataloged item <- Any tracking detail request along with libraryItemId > 0 or libraryItemId != null
        var totalCatalogedItem = groupedDetail.Count(wtd => wtd.LibraryItemId != null && wtd.LibraryItemId > 0);
        // Total instance of cataloged item  
        var totalCatalogedInstanceItem = groupedDetail
            .Where(wtd => wtd.LibraryItemId != null && wtd.LibraryItemId > 0 && wtd.HasGlueBarcode)
            .Select(wtd => wtd.ItemTotal).Sum();
        
        // Total price
        var totalPrice = groupedDetail.Select(g => g.TotalAmount).Sum();

        return groupedDetail.Any()
            ? new()
            {
                Category = category,
                TotalItem = totalItem,
                TotalCatalogedItem = totalCatalogedItem,
                TotalInstanceItem = totalInstanceItem,
                TotalCatalogedInstanceItem = totalCatalogedInstanceItem,
                TotalPrice = totalPrice
            }
            : new()
            {
                Category = category,
                TotalItem = null,
                TotalCatalogedItem = null,
                TotalInstanceItem = null,
                TotalCatalogedInstanceItem = null,
                TotalPrice = null
            };
    }
}