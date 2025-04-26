using System.Text.Json.Serialization;
using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Application.Dtos.Fine;

public class FinePolicyDto
{
    // Key
    public int FinePolicyId { get; set; }
    
    // Policy detail information
    public string FinePolicyTitle { get; set; } = null!;    
    public FinePolicyConditionType ConditionType { get; set; } 
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
    
    #region Archived Properties
    // public decimal? FineAmountPerDay { get; set; }
    // public decimal? FixedFineAmount { get; set; }
    #endregion

    // Mapping entity
    [JsonIgnore]
    public ICollection<FineDto> Fines { get; set; } = new List<FineDto>();
}