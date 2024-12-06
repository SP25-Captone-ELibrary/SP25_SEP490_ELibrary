namespace FPTU_ELibrary.Domain.Specifications.Params;

public class RoleSpecParams : BaseSpecParams
{
    public int? CategoryId { get; set; }
    public int? RoleId { get; set; }
    public int? FeatureId { get; set; }
    public int? PermissionId { get; set; }
}