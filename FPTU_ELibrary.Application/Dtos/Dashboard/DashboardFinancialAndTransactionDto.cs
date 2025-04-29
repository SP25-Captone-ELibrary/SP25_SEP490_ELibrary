using FPTU_ELibrary.Application.Dtos.Payments;
using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Application.Dtos.Dashboard;

public class DashboardFinancialAndTransactionDto
{
    public List<DashboardFinancialAndTransactionDetailDto> Details { get; set; } = null!;
    public decimal TotalRevenueLastYear { get; set; }
    public decimal TotalRevenueThisYear { get; set; }
    public PaginatedResultDto<GetTransactionDto> LatestTransactions { get; set; } = null!;
}

public class DashboardFinancialAndTransactionDetailDto
{
    public TransactionType TransactionType { get; set; }
    public double PendingPercentage { get; set; }
    public double PaidPercentage { get; set; }
    public double ExpiredPercentage { get; set; }
    public double CancelledPercentage { get; set; }
    public decimal TotalRevenueLastYear { get; set; }
    public decimal TotalRevenueThisYear { get; set; }
    public List<BarchartTrendDataDto> LastYear { get; set; } = new();
    public List<BarchartTrendDataDto> ThisYear { get; set; } = new();
}