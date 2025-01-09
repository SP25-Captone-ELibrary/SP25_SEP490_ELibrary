namespace FPTU_ELibrary.API.Payloads.Requests.Fine;

public class CreateFinePolicyRequest
{
    public string ConditionType { get; set; } = null!;
    public string FineAmountPerDay { get; set; }
    public string? FixedFineAmount { get; set; }
    public string? Description { get; set; }
}
