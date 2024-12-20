using FPTU_ELibrary.Application.Dtos.Employees;

namespace FPTU_ELibrary.Application.Dtos.Books;

public class CopyConditionHistoryDto
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
    public BookEditionCopyDto BookEditionCopy { get; set; } = null!;
    public EmployeeDto ChangedByNavigation { get; set; } = null!;
}