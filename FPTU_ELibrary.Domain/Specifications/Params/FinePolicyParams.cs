namespace FPTU_ELibrary.Domain.Specifications.Params;

public class FinePolicyParams : BaseSpecParams
{
    public string? ConditionType { get; set; } = null!;
    public decimal? FineAmountPerDay { get; set; }
    public decimal? FixedFineAmount { get; set; }
    public string? Description { get; set; }
}


