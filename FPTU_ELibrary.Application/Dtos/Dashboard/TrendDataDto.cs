namespace FPTU_ELibrary.Application.Dtos.Dashboard;

public class TrendDataDto
{
    public string PeriodLabel { get; set; } = null!;
    public int Value { get; set; }
}

public class BarchartTrendDataDto
{
    public string PeriodLabel { get; set; } = null!;
    public decimal Value { get; set; }
}
