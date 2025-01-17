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
    public decimal FineAmountPerDay { get; set; }
    public decimal? FixedFineAmount { get; set; }
    public string? Description { get; set; }

    // Mapping entity
    [JsonIgnore]
    public ICollection<FineDto> Fines { get; set; } = new List<FineDto>();
}