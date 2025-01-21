using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.API.Payloads.Requests.User;

public class UpdateUserRequest
{
    // Student detail and credentials information
    public string? UserCode { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public DateTime? Dob { get; set; }
    public string Phone { get; set; } = null!;
    public string Address { get; set; } = null!;
    public Gender Gender { get; set; }

    // Recognise who update account
    // public string ModifyBy { get; set; } = null!;
    // public string? Avatar { get; set; } 
}