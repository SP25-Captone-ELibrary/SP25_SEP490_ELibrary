using FPTU_ELibrary.Application.Dtos.Employees;
using FPTU_ELibrary.Domain.Entities.Base;

namespace FPTU_ELibrary.Application.Dtos.Auth
{
    public class AuthenticateUserDto : BaseUser
    {
        public Guid Id { get; set; }
        public string? UserCode { get; set; }
        public string? Password { get; set; } 
        public int RoleId { get; set; }
        public string RoleName { get; set; } = null!;
        public bool IsEmployee { get; set; }
    }

    public static class AuthenticateUserDtoExtensions
    {
        public static UserDto ToUserDto(
            this AuthenticateUserDto authenticateUser)
        {
			return new UserDto()
            {
                UserId = authenticateUser.Id,
                UserCode = authenticateUser.UserCode,
                FirstName = authenticateUser.FirstName,
                LastName = authenticateUser.LastName,
                Email = authenticateUser.Email,
                Avatar = authenticateUser.Avatar,
                Address = authenticateUser.Address,
                Gender = authenticateUser.Gender,
                Dob = authenticateUser.Dob,
                Phone = authenticateUser.Phone,
                PasswordHash = authenticateUser.PasswordHash,
                RoleId = authenticateUser.RoleId,
                CreateDate = authenticateUser.CreateDate,
                ModifiedDate = authenticateUser.ModifiedDate,
                ModifiedBy = authenticateUser.ModifiedBy,
                EmailConfirmed = authenticateUser.EmailConfirmed,
                PhoneNumberConfirmed = authenticateUser.PhoneNumberConfirmed,
                EmailVerificationCode = authenticateUser.EmailVerificationCode,
                TwoFactorEnabled = authenticateUser.TwoFactorEnabled,
                TwoFactorSecretKey = authenticateUser.TwoFactorSecretKey,
                TwoFactorBackupCodes = authenticateUser.TwoFactorBackupCodes,
                PhoneVerificationCode = authenticateUser.PhoneVerificationCode,
                PhoneVerificationExpiry = authenticateUser.PhoneVerificationExpiry,
				IsActive = authenticateUser.IsActive,
                IsDeleted = authenticateUser.IsDeleted
            };
        }

        public static EmployeeDto ToEmployeeDto(
            this AuthenticateUserDto authenticateUser)
        {
            return new EmployeeDto()
            {
                EmployeeId = authenticateUser.Id,
                EmployeeCode = authenticateUser.UserCode,
                FirstName = authenticateUser.FirstName,
                LastName = authenticateUser.LastName,
                Email = authenticateUser.Email,
                Avatar = authenticateUser.Avatar,
                Address = authenticateUser.Address,
                Gender = authenticateUser.Gender,
                Dob = authenticateUser.Dob,
                Phone = authenticateUser.Phone,
                PasswordHash = authenticateUser.PasswordHash,
                RoleId = authenticateUser.RoleId,
                CreateDate = authenticateUser.CreateDate,
                ModifiedDate = authenticateUser.ModifiedDate,
                ModifiedBy = authenticateUser.ModifiedBy,
                EmailConfirmed = authenticateUser.EmailConfirmed,
                PhoneNumberConfirmed = authenticateUser.PhoneNumberConfirmed,
                EmailVerificationCode = authenticateUser.EmailVerificationCode,
                TwoFactorEnabled = authenticateUser.TwoFactorEnabled,
                TwoFactorSecretKey = authenticateUser.TwoFactorSecretKey,
                TwoFactorBackupCodes = authenticateUser.TwoFactorBackupCodes,
                PhoneVerificationCode = authenticateUser.PhoneVerificationCode,
                PhoneVerificationExpiry = authenticateUser.PhoneVerificationExpiry,
                IsActive = authenticateUser.IsActive,
                IsDeleted = authenticateUser.IsDeleted
            };
        }
    }
}
