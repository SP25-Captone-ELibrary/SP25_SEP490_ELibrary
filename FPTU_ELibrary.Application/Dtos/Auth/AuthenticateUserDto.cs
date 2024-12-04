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
                PasswordHash = authenticateUser.Password,
                RoleId = authenticateUser.RoleId,
                CreateDate = authenticateUser.CreateDate,
                EmailConfirmed = authenticateUser.EmailConfirmed,
                PhoneNumberConfirmed = authenticateUser.PhoneNumberConfirmed,
                TwoFactorEnabled = authenticateUser.TwoFactorEnabled,
				IsActive = authenticateUser.IsActive,
            };
        }
    }
}
