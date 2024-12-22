namespace FPTU_ELibrary.Application.Dtos.Fine;

public class FinePolicyExcelRecord
{
    public string? ConditionType { get; set; } = null!;
    public decimal? FineAmountPerDay { get; set; }
    public decimal? FixedFineAmount { get; set; }
    public string? Description { get; set; }
}

public class FinePolicyFailedMessage
{
    public int Row { get; set; }
    public List<string> ErrMsg { get; set; } = null!;
}

public static class FinePolicyExcelRecordExtension
{
    public static FinePolicyDto ToFinePolicyDto(this FinePolicyExcelRecord req)
    {
        return new FinePolicyDto()
        {
            ConditionType = req.ConditionType,
            FineAmountPerDay = req.FineAmountPerDay??0,
            FixedFineAmount = req.FixedFineAmount,
            Description = req.Description
        };
    }
    
    public static List<FinePolicyExcelRecord> ToFinePolicyExcelRecords(this IEnumerable<FinePolicyDto> finePolicies)
    {
        return finePolicies.Select(e =>
        {
            return new FinePolicyExcelRecord()
            {
                ConditionType = e.ConditionType,
                FineAmountPerDay = e.FineAmountPerDay,
                FixedFineAmount = e.FixedFineAmount,
                Description = e.Description
            };
        }).ToList();
    }
}