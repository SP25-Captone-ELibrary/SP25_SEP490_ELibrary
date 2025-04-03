using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Application.Dtos.WarehouseTrackings;

namespace FPTU_ELibrary.Application.Dtos.Dashboard;

public class DashboardInventoryAndStockDto
{
    public InventorySummaryDto InventoryStockSummary { get; set; } = null!;
    public List<InventoryCategorySummaryDto> InventoryStockCategorySummary { get; set; } = new();
}

public class InventorySummaryDto
{
    public int TotalStockInItem { get; set; }
    public int TotalInstanceItem { get; set; } 
    public int TotalCatalogedItem { get; set; } 
    public int TotalCatalogedInstanceItem { get; set; } 
}

public class InventoryCategorySummaryDto
{
    public CategoryDto Category { get; set; } = null!;
    public int TotalStockInItem { get; set; }
    public int TotalInstanceItem { get; set; } 
    public int TotalCatalogedItem { get; set; } 
    public int TotalCatalogedInstanceItem { get; set; } 
}
