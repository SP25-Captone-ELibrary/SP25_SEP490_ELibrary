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
            this AuthenticateUserDto authenticateUser,
            Guid? userId = null,
            bool? isSignUpFromExternalProvider = false)
        {
			var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
				// Vietnam timezone
				TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));

			return new UserDto()
            {
                UserId = userId ?? Guid.NewGuid(),
                UserCode = authenticateUser.UserCode,
                FirstName = authenticateUser.FirstName,
                LastName = authenticateUser.LastName,
                Email = authenticateUser.Email,
                Avatar = authenticateUser.Avatar,
                PasswordHash = authenticateUser.Password ?? null!,
                RoleId = authenticateUser.RoleId,
                CreateDate = currentLocalDateTime,
                EmailConfirmed = isSignUpFromExternalProvider ?? false,
                PhoneNumberConfirmed = false,
                TwoFactorEnabled = false,

				// Default is inactive, active as when user perform email confirmed success
                // Default is active if sign up with external provider
				IsActive = isSignUpFromExternalProvider ?? false,
            };
        }
    }
}
