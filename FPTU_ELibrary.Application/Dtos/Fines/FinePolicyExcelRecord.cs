using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Application.Dtos.Fine;

public class FinePolicyExcelRecord
{
    public string FinePolicyTitle { get; set; } = null!;
    public string? ConditionType { get; set; }
    public string? Description { get; set; }
    
    #region Handle Damage
    public decimal? MinDamagePct { get; set; }
    public decimal? MaxDamagePct { get; set; }
    public decimal? ProcessingFee { get; set; }
    #endregion

    #region Handle Overdue
    public decimal? DailyRate { get; set; }
    #endregion

    #region Handle Lost & Damage
    public decimal? ChargePct { get; set; }
    #endregion
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
            MinDamagePct = req.MinDamagePct,
            MaxDamagePct = req.MaxDamagePct,
            ProcessingFee = req.ProcessingFee,
            DailyRate = req.DailyRate,
            ChargePct = req.ChargePct,
            Description = req.Description
        };
    }
    
    public static List<FinePolicyExcelRecord> ToFinePolicyExcelRecords(this IEnumerable<FinePolicyDto> finePolicies)
    {
        return finePolicies.Select(e => new FinePolicyExcelRecord()
        {
            ConditionType = e.ConditionType.ToString(),
            MinDamagePct = e.MinDamagePct,
            MaxDamagePct = e.MaxDamagePct,
            ProcessingFee = e.ProcessingFee,
            DailyRate = e.DailyRate,
            ChargePct = e.ChargePct,
            Description = e.Description
        }).ToList();
    }
}