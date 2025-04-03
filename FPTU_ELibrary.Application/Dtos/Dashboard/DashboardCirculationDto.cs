using FPTU_ELibrary.Application.Dtos.LibraryItems;

namespace FPTU_ELibrary.Application.Dtos.Dashboard;

public class DashboardCirculationDto
{
    // Request/Reservation circulation units
    public int TotalRequestUnits { get; set; }
    public int TotalReservedUnits { get; set; }
    
    // Failed rates 
    public int TotalBorrowFailed { get; set; }    
    public double BorrowFailedRates { get; set; }    
    // Failed rates for each category
    public List<BorrowFailedCategorySummaryDto> CategoryBorrowFailedSummary { get; set; } = new();
    
    // Overdue rates
    public int TotalOverdue { get; set; }
    public double OverdueRates { get; set; }
    // Overdue rates for each category
    public List<OverdueCategorySummaryDto> CategoryOverdueSummary { get; set; } = new();
    
    // Lost rate
    public int TotalLost { get; set; }
    public double LostRates { get; set; }
    // Lost rates for each category
    public List<LostCategorySummaryDto> CategoryLostSummary { get; set; } = new();
    
    // Borrow & return trends
    public List<TrendDataDto> BorrowTrends { get; set; } = new();
    public List<TrendDataDto> ReturnTrends { get; set; } = new();
}

public class BorrowFailedCategorySummaryDto
{
    public CategoryDto Category { get; set; } = null!;
    public int TotalBorrowFailed { get; set; }    
    public double BorrowFailedRates { get; set; }    
}

public class OverdueCategorySummaryDto
{
    public CategoryDto Category { get; set; } = null!;
    public int TotalOverdue { get; set; }
    public double OverdueRates { get; set; }
}

public class LostCategorySummaryDto
{
    public CategoryDto Category { get; set; } = null!;
    public int TotalLost { get; set; }
    public double LostRates { get; set; }
}