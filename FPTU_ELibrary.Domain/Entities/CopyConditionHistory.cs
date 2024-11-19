namespace FPTU_ELibrary.Domain.Entities;

public class CopyConditionHistory
{
    // Key
    public int ConditionHistoryId { get; set; }

    // Condition history for which book edition
    public int BookEditionCopyId { get; set; }

    // Record management properties
    public string Condition { get; set; } = null!;
    public DateTime ChangeDate { get; set; }
    public Guid ChangedBy { get; set; }

    // Mapping entities
    public BookEditionCopy BookEditionCopy { get; set; } = null!;
    public Employee ChangedByNavigation { get; set; } = null!;
}
