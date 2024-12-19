using CsvHelper.Configuration.Attributes;
using FPTU_ELibrary.Application.Dtos;

namespace FPTU_ELibrary.Application;

public class UserExcelRecord
{
    [Name("Email")]
    public string Email { get; set; } = null!;
    
    [Name("FirstName")]
    public string FirstName { get; set; } = null!;
    
    [Name("LastName")]
    public string LastName { get; set; } = null!;
    
    [Name("Phone")]
    public string? Phone { get; set; }
    
    [Name("Address")]
    public string? Address { get; set; }
    
    [Name("Gender")]
    public string? Gender { get; set; }
    
    [Name("Dob")]
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
    
    public static List<UserExcelRecord> ToUserExcelRecords(this IEnumerable<UserDto> users)
    {
        return users.Select(e =>
        {
            return new UserExcelRecord()
            {
                Email = e.Email,
                FirstName = e.FirstName ?? string.Empty,
                LastName = e.LastName ?? string.Empty,
                Phone = e.Phone,
                Address = e.Address,
                Gender = e.Gender,
                Dob = e.Dob?.ToString("yyyy-MM-dd"),
            };
        }).ToList();
    }
}