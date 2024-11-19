using System.Text.Json.Serialization;

namespace FPTU_ELibrary.Domain.Entities;

public class FinePolicy
{
    // Key
    public int FinePolicyId { get; set; }
    
    // Policy detail information
    public string ConditionType { get; set; } = null!;
    public decimal FineAmountPerDay { get; set; }
    public decimal? FixedFineAmount { get; set; }
    public string? Description { get; set; }

    // Mapping entity
    [JsonIgnore]
    public ICollection<Fine> Fines { get; set; } = new List<Fine>();
}
