using FPTU_ELibrary.Application.Dtos.Payments;

namespace FPTU_ELibrary.Application.Dtos.Dashboard;

public class DashboardFinancialAndTransactionDto
{
    public List<BarchartTrendDataDto> LastYear { get; set; } = new();
    public List<BarchartTrendDataDto> ThisYear { get; set; } = new();
    public decimal TotalRevenueLastYear { get; set; }
    public decimal TotalRevenueThisYear { get; set; }
    public PaginatedResultDto<GetTransactionDto> LatestTransactions { get; set; } = null!;
}