namespace FPTU_ELibrary.Application.Dtos.Auth
{
    public class AuthenticatedUserDto
    {
        public Guid Id { get; set; }
        public string? UserCode { get; set; }
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public int RoleId { get; set; }
        public string RoleName { get; set; } = null!;
        public string? Avatar { get; set; }
        public bool IsActive { get; set; }
        public bool IsEmployee { get; set; }
    }

    public static class AuthenticatedUserDtoExtensions
    {
        public static UserDto ToUserDto(this AuthenticatedUserDto authenticatedUser)
        {
			var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
				// Vietnam timezone
				TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));

			return new UserDto()
            {
                UserCode = authenticatedUser.UserCode,
                FirstName = authenticatedUser.FirstName,
                LastName = authenticatedUser.LastName,
                Email = authenticatedUser.Email,
                PasswordHash = authenticatedUser.Password,
                RoleId = authenticatedUser.RoleId,
                CreateDate = currentLocalDateTime,
                EmailConfirmed = false,
                PhoneNumberConfirmed = false,
                TwoFactorEnabled = false,

				// Default is inactive, active as when user perform email confirmed success
				IsActive = false
            };
        }
    }
}
