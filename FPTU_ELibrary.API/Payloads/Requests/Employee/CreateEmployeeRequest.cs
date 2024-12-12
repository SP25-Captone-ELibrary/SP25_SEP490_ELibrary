using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.API.Payloads.Requests.Employee;

public class CreateEmployeeRequest
{
    public string? EmployeeCode { get; set; }
    public string Email { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public DateTime Dob { get; set; }
    public string Phone { get; set; } = null!;
    public string Address { get; set; } = null!;
    public Gender Gender { get; set; }
    public DateTime HireDate { get; set; }
    public int RoleId { get; set; }
}