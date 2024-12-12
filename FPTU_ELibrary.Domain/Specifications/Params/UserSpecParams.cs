namespace FPTU_ELibrary.Domain.Specifications.Params;

public class UserSpecParams : BaseSpecParams
{
    public string? UserCode { get; set; }
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
}