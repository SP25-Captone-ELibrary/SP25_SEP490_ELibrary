namespace FPTU_ELibrary.API.Payloads.Requests.Fine;

public class UpdateFinePolicyRequest
{
    public string? ConditionType { get; set; } = null!;
    public decimal? FineAmountPerDay { get; set; }
    public decimal? FixedFineAmount { get; set; }
    public string? Description { get; set; }
}