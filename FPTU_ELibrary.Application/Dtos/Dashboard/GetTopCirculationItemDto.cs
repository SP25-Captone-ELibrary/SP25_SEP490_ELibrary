using FPTU_ELibrary.Application.Dtos.LibraryItems;

namespace FPTU_ELibrary.Application.Dtos.Dashboard;

public class GetTopCirculationItemDto
{
    public LibraryItemDetailDto LibraryItem { get; set; } = null!;
    
    // Circulation summary fields
    public int BorrowSuccessCount { get; set; }
    public int BorrowRequestCount { get; set; }
    public int BorrowFailedCount { get; set; }
    public int TotalSatisfactionUnits { get; set; }
    public double SatisfactionRate { get; set; }
    public double BorrowExtensionRate { get; set; }
    
    // public int DigitalBorrowCount { get; set; }
    // public int ExtendedBorrowCount { get; set; }
    // public int ReserveCount { get; set; }
    // public double BorrowFailedRate { get; set; }
    
    public AvailableVsNeedChartItemDto AvailableVsNeedChart { get; set; } = null!; 
    public List<TrendDataDto> BorrowTrends { get; set; } = new();
    public List<TrendDataDto> ReservationTrends { get; set; } = new();
}

public class AvailableVsNeedChartDto
{
    public int AvailableUnits { get; set; }
    public int NeedUnits { get; set; }
}

public class AvailableVsNeedChartItemDto : AvailableVsNeedChartDto
{
    public double AverageNeedSatisfactionRate { get; set; }
}

public class AvailableVsNeedChartCategoryDto : AvailableVsNeedChartItemDto
{
    public CategoryDto Category { get; set; } = null!;
    public int TotalInShelf { get; set; }
    public int TotalOutOfShelf { get; set; }
    public int TotalRequest { get; set; }
    public int TotalReserved { get; set; }
    // public int TotalBorrowed { get; set; }
    // public int TotalRequestFailed { get; set; }
}

public class DashboardTopCirculationItemDto
{
    public PaginatedResultDto<GetTopCirculationItemDto> TopBorrowItems { get; set; } = null!;
    public List<AvailableVsNeedChartCategoryDto> AvailableVsNeedChartCategories { get; set; } = new();
    public AvailableVsNeedChartDto AvailableVsNeedChartSummary { get; set; } = null!;
}