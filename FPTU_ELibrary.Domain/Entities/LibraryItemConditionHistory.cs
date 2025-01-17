using FPTU_ELibrary.Domain.Interfaces;

namespace FPTU_ELibrary.Domain.Entities;

public class LibraryItemConditionHistory : IAuditableEntity
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
    public LibraryItemInstance LibraryItemInstance { get; set; } = null!;
}
