using FPTU_ELibrary.Application.Dtos.Auth;
using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.API.Payloads.Requests.Employee;

public class UpdateProfileRequest
{
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public DateTime? Dob { get; set; }
    public string Phone { get; set; } = null!;
    public string Address { get; set; } = null!;
    public Gender Gender { get; set; }
    public string? Avatar { get; set; } 
}

public static class UpdateProfileRequestExtensions
{
    public static AuthenticateUserDto ToAuthenticateUserDto(
        this UpdateProfileRequest request,
        string email,
        bool isEmployee)
    {
        return new AuthenticateUserDto
        {
            Email = email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Dob = request.Dob,
            Phone = request.Phone,
            Address = request.Address,
            Gender = request.Gender.ToString(),
            Avatar = request.Avatar,
            IsEmployee = isEmployee
        };
    }
}