using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.API.Payloads.Requests.Dashboard;

public class DashboardFilterRequest
{
    // Optional start and end date filters
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    // Period grouping
    public TrendPeriod Period { get; set; } = TrendPeriod.Daily;
}