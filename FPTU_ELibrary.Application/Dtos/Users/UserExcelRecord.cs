using FPTU_ELibrary.Application.Dtos;

namespace FPTU_ELibrary.Application;

public class UserExcelRecord
{
    public string Email { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Gender { get; set; }
    public string? Dob { get; set; }
}

public class UserFailedMessage
{
    public int Row { get; set; }
    public List<string> ErrMsg { get; set; } = null!;
}

public static class UserExcelRecordExtension
{
    public static UserDto ToUserDto(this UserExcelRecord req)
    {
        return new UserDto()
        {
            CreateDate = DateTime.Now,
            Email = req.Email,
            FirstName = req.FirstName,
            LastName = req.LastName,
            Phone = req.Phone,
            Address = req.Address,
            Gender = req.Gender,
            Dob = DateTime.TryParse(req.Dob, out var dob) == true ? dob : null,
            IsActive = true,
            IsDeleted = false,
            EmailConfirmed = false,
            TwoFactorEnabled = false,
            PhoneNumberConfirmed = false
        };
    }
}