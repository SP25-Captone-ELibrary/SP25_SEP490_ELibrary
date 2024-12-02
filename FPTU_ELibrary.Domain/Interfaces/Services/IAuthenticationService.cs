using FPTU_ELibrary.Domain.Interfaces.Services.Base;

namespace FPTU_ELibrary.Domain.Interfaces.Services
{
    public interface IAuthenticationService<TDto>
        where TDto : class
    {
        Task<IServiceResult> SignInAsync(TDto user, bool isSignInFromExternalProvider);
        Task<IServiceResult> SignUpAsync(TDto user);
        Task<IServiceResult> ConfirmEmailAsync(string email, string emailVerificationCode);
        Task<IServiceResult> RefreshTokenAsync(string email, string userType, string name,
            string roleName, string tokenId, string refreshToken);
	}
}
