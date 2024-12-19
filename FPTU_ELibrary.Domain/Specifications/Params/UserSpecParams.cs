namespace FPTU_ELibrary.Domain.Specifications.Params;

public class UserSpecParams : BaseSpecParams
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Gender { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsDeleted { get; set; }
    public DateTime[]? DobRange { get; set; } 
    public DateTime[]? CreateDateRange { get; set; } 
    public DateTime[]? ModifiedDateRange { get; set; } 
}