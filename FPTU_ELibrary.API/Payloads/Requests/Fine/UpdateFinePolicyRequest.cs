using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.API.Payloads.Requests.Fine;

public class UpdateFinePolicyRequest
{
    public string FinePolicyTitle { get; set; } = null!;
    public FinePolicyConditionType ConditionType { get; set; }
    public string? Description { get; set; }
    
    #region Handle Damage
    public decimal? MinDamagePct { get; set; }
    public decimal? MaxDamagePct { get; set; }
    public decimal? ChargePct { get; set; }
    public decimal? ProcessingFee { get; set; }
    #endregion

    #region Handle Overdue
    public decimal? DailyRate { get; set; }
    #endregion

    #region Handle Lost
    public decimal? ReplacementFeePercentage { get; set; }
    #endregion
}