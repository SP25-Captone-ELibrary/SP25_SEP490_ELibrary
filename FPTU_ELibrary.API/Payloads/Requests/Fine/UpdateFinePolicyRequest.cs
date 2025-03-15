using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.API.Payloads.Requests.Fine;

public class UpdateFinePolicyRequest
{
    public string FinePolicyTitle { get; set; } = null!;
    public FinePolicyConditionType ConditionType { get; set; }
    public decimal? FineAmountPerDay { get; set; } 
    public decimal? FixedFineAmount { get; set; }
    public string? Description { get; set; }
}