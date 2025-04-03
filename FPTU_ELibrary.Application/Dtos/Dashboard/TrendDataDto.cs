namespace FPTU_ELibrary.Application.Dtos.Dashboard;

public enum TrendPeriod
{
    Daily,
    Weekly,
    Monthly,
    YearComparision
}

public class TrendDataDto
{
    public string PeriodLabel { get; set; } = null!;
    public int Count { get; set; }
}

public class BarchartTrendDataDto
{
    public string PeriodLabel { get; set; } = null!;
    public decimal Count { get; set; }
}
