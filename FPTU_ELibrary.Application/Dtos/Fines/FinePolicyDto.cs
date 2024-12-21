namespace FPTU_ELibrary.Application.Dtos.Fine;

public class FinePolicyDto
{
    public int FinePolicyId { get; set; }
    
    // Policy detail information
    public string ConditionType { get; set; } = null!;
    public decimal FineAmountPerDay { get; set; }
    public decimal? FixedFineAmount { get; set; }
    public string? Description { get; set; }

    //public ICollection<FineDto> Fines { get; set; } = new List<Fine>();
}