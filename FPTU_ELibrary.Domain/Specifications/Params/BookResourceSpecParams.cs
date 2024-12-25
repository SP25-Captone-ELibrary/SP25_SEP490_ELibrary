namespace FPTU_ELibrary.Domain.Specifications.Params;

public class BookResourceSpecParams : BaseSpecParams
{
    public string? ResourceType { get; set; }
    public string? FileFormat { get; set; }
    public string? Provider { get; set; }
    public bool? IsDeleted { get; set; }
    public DateTime[]? LastCreatedAtRange { get; set; }
    public DateTime[]? LastUpdatedAtRange { get; set; }
}