namespace FPTU_ELibrary.Domain.Specifications.Params;

public class AuditTrailSpecParams : BaseSpecParams
{
    public string EntityId { get; set; } = null!;
    public string EntityName { get; set; } = null!;
}