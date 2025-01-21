using System.Text.Json.Serialization;

namespace FPTU_ELibrary.Application.Dtos.LibraryItems;

public class LibraryItemConditionHistoryDto
{
    // Key
    public int ConditionHistoryId { get; set; }

    // Condition history for which item instance
    public int LibraryItemInstanceId { get; set; }

    // Record management properties
    public string Condition { get; set; } = null!;

    // Creation and update datetime
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = null!;
    public string? UpdatedBy { get; set; }
    
    // Mapping entities
    [JsonIgnore]
    public LibraryItemInstanceDto LibraryItemInstance { get; set; } = null!;
}