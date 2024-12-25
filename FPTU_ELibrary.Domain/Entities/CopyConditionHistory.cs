using FPTU_ELibrary.Domain.Interfaces;

namespace FPTU_ELibrary.Domain.Entities;

public class CopyConditionHistory : IAuditableEntity
{
    // Key
    public int ConditionHistoryId { get; set; }

    // Condition history for which book edition
    public int BookEditionCopyId { get; set; }

    // Record management properties
    public string Condition { get; set; } = null!;

    // Creation and update datetime
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = null!;
    public string? UpdatedBy { get; set; }
    
    // Mapping entities
    public BookEditionCopy BookEditionCopy { get; set; } = null!;
}
