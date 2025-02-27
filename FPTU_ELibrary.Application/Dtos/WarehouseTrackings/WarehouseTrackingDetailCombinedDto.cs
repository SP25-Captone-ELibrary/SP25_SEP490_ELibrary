using FPTU_ELibrary.Application.Dtos.LibraryItems;

namespace FPTU_ELibrary.Application.Dtos.WarehouseTrackings;

public class WarehouseTrackingDetailCombinedDto
{
    public PaginatedResultDto<WarehouseTrackingDetailDto> Result { get; set; } = null!;
    public List<WarehouseTrackingCategorySummaryDto> Statistics { get; set; } = new();
    public WarehouseTrackingInventoryDto StatisticSummary { get; set; } = null!;
}

public static class WarehouseTrackingDetailCombinedDtoExtensions
{
    public static WarehouseTrackingDetailCombinedDto ToDetailCombinedDto(
        this List<WarehouseTrackingDetailDto> dtos,
        WarehouseTrackingDto trackingDto,
        List<CategoryDto> categories,
        int pageIndex, int pageSize, int totalPage, int totalActualItem)
    {
        // Initialize collection of statistic 
        List<WarehouseTrackingCategorySummaryDto> statisticDtos = new();
        
        // Iterate each category 
        foreach (var cate in categories)
        {
            // Group warehouse tracking detail by category
            var statisticSummary = dtos
                .Where(g => g.CategoryId == cate.CategoryId)
                .GroupBy(g => g.CategoryId)
                .Select(g => g.ToList().ToSummaryDto(category: g.First().Category))
                .FirstOrDefault();

            // Add to collection
            if (statisticSummary != null) statisticDtos.Add(statisticSummary);
            // Add default summary 
            else statisticDtos.Add(new WarehouseTrackingCategorySummaryDto()
            {
                Category = cate
            });
        }

        // Pagination result 
        var paginationResultDto = new PaginatedResultDto<WarehouseTrackingDetailDto>(dtos, 
            pageIndex, pageSize, totalPage, totalActualItem);
        
        return new()
        {
            Result = paginationResultDto,
            Statistics = statisticDtos.ToList(),
            StatisticSummary = trackingDto.WarehouseTrackingInventory
        };
    }
}