using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Application.Dtos.Fine;

public class FinePolicyExcelRecord
{
    public string FinePolicyTitle { get; set; } = null!;
    public string? ConditionType { get; set; } 
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
        // Try parse condition type
        Enum.TryParse(typeof(FinePolicyConditionType), req.ConditionType, out var validEnum);
        
        return new FinePolicyDto()
        {
            FinePolicyTitle = req.FinePolicyTitle,
            ConditionType = (FinePolicyConditionType) validEnum!,
            FineAmountPerDay = req.FineAmountPerDay ??0,
            FixedFineAmount = req.FixedFineAmount,
            Description = req.Description
        };
    }
    
    public static List<FinePolicyExcelRecord> ToFinePolicyExcelRecords(this IEnumerable<FinePolicyDto> finePolicies)
    {
        return finePolicies.Select(e => new FinePolicyExcelRecord()
        {
            ConditionType = e.ConditionType.ToString(),
            FineAmountPerDay = e.FineAmountPerDay,
            FixedFineAmount = e.FixedFineAmount,
            Description = e.Description
        }).ToList();
    }
}