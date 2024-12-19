namespace FPTU_ELibrary.Domain.Specifications.Params;

public class AuthorSpecParams : BaseSpecParams
{
    public string? AuthorCode { get; set; }
    public string? Nationality { get; set; }
    public bool? IsDeleted { get; set; }
    public DateTime[]? DobRange { get; set; } 
    public DateTime[]? DateOfDeathRange { get; set; }
    public DateTime[]? CreateDateRange { get; set; } 
    public DateTime[]? ModifiedDateRange { get; set; } 
}