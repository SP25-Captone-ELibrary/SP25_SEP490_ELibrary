namespace FPTU_ELibrary.API.Payloads.Requests.Fine;

public class CreateFineRequest
{
    public int FinePolicyId { get; set; }
}

public class CreateLostFineRequest : CreateFineRequest
{
    public string? FineNote { get; set; }
    public decimal FineAmount { get; set; } = decimal.Zero;
}