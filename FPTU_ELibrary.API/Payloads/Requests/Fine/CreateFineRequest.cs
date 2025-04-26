namespace FPTU_ELibrary.API.Payloads.Requests.Fine;

public class CreateFineRequest
{
    public string? FineNote { get; set; }
    public decimal? DamagePct { get; set; }
    public int FinePolicyId { get; set; }
}
