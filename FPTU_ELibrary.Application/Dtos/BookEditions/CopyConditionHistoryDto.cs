namespace FPTU_ELibrary.Application.Dtos.BookEditions;

public class CopyConditionHistoryDto
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
    public BookEditionCopyDto BookEditionCopy { get; set; } = null!;
}