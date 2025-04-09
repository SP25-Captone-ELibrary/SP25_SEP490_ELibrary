using FPTU_ELibrary.Application.Dtos.LibraryItems;

namespace FPTU_ELibrary.Application.Dtos.Dashboard;

public class DashboardDigitalResourceDto
{
    public int TotalDigitalResource { get; set; }
    public int TotalActiveDigitalBorrowing { get; set; }
    public double ExtensionRatePercentage { get; set; }
    public double AverageExtensionsPerBorrow { get; set; }
    public PaginatedResultDto<GetTopBorrowResourceDto> TopBorrowLibraryResources { get; set; } = null!;
}