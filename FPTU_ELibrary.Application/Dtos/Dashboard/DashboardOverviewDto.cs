namespace FPTU_ELibrary.Application.Dtos.Dashboard;

public class DashboardOverviewDto
{
    // Inventory
    public int TotalItemUnits { get; set; }
    public int TotalDigitalUnits { get; set; }
    public int TotalBorrowingUnits { get; set; }
    public int TotalOverdueUnits { get; set; }
    public int TotalPatrons { get; set; }
    public int TotalInstanceUnits { get; set; }
    public int TotalAvailableUnits { get; set; }
    public int TotalLostUnits { get; set; }
}