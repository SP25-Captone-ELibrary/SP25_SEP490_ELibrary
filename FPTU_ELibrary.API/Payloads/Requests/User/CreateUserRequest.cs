using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.API.Payloads.Requests.Auth;

public class CreateUserRequest
{
    public string Email { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public DateTime Dob { get; set; }
    public string Phone { get; set; } = null!;
    public string Address { get; set; } = null!;
    public Gender Gender { get; set; }
}