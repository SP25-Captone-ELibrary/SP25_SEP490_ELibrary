namespace FPTU_ELibrary.Domain.Interfaces;

public interface IAuditableEntity
{
    DateTime CreatedAt { get; set; }
    
    DateTime? UpdatedAt { get; set; }

    string CreatedBy { get; set; }

    string? UpdatedBy { get; set; }
}