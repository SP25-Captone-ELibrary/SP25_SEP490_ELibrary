using CsvHelper.Configuration.Attributes;
using FPTU_ELibrary.Application.Dtos.Roles;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;

namespace FPTU_ELibrary.Application.Dtos.Employees;

public class EmployeeCsvRecord
{
    [Name("Email")]
    public string Email { get; set; } = null!;
    
    [Name("EmployeeCode")]
    public string? EmployeeCode { get; set; }
    
    [Name("FirstName")]
    public string FirstName { get; set; } = null!;
    
    [Name("LastName")]
    public string LastName { get; set; } = null!;
    
    [Name("Dob")]
    public string? Dob { get; set; }
    
    [Name("Phone")]
    public string? Phone { get; set; }
    
    [Name("Address")]
    public string? Address { get; set; }
    
    [Name("Gender")]
    public string? Gender { get; set; }
    
    [Name("HireDate")]
    public string? HireDate { get; set; }
    
    [Name("TerminationDate")]
    public string? TerminationDate { get; set; }

    [Name("Role")]
    public string Role { get; set; } = null!;
}

public static class EmployeeCsvRecordExtensions
{
    public static List<EmployeeDto> ToEmployeeDtosForImport(this IEnumerable<EmployeeCsvRecord> records, 
        List<SystemRoleDto> employeeRoles)
    {
        // Current local datetime
        var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
            // Vietnam timezone
            TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
        
        return records.Select(rc =>
        {
            var role = employeeRoles.FirstOrDefault(x => x.EnglishName.Equals(rc.Role,
                StringComparison.OrdinalIgnoreCase));
            if (role == null) return null!;

            return new EmployeeDto()
            {
                Email = rc.Email,
                EmployeeCode = rc.EmployeeCode,
                FirstName = rc.FirstName,
                LastName = rc.LastName,
                Phone = rc.Phone,
                Address = rc.Address,
                Gender = rc.Gender,
                Dob = !string.IsNullOrEmpty(rc.Dob) 
                    ? DateTime.Parse(rc.Dob) : null,
                HireDate = !string.IsNullOrEmpty(rc.HireDate) 
                    ? DateTime.Parse(rc.HireDate) : null,
                TerminationDate = !string.IsNullOrEmpty(rc.TerminationDate)  
                    ? DateTime.Parse(rc.TerminationDate) : null,

                // Default account settings
                IsActive = false,
                IsDeleted = false,
                EmailConfirmed = false,
                TwoFactorEnabled = false,
                PhoneNumberConfirmed = false,
                CreateDate = currentLocalDateTime,
                RoleId = role.RoleId
            };
        }).ToList();
    }
}