using FPTU_ELibrary.Domain.Interfaces.Services.Base;

namespace FPTU_ELibrary.Domain.Interfaces.Services
{
    public interface IAuthenticationService<TDto>
        where TDto : class
    {
        Task<IServiceResult> SignInAsync(TDto user);
        Task<IServiceResult> SignUpAsync(TDto user, bool isSignUpFromExternal);
        Task<IServiceResult> ConfirmEmailAsync(string email, string emailVerificationCode);
        Task<IServiceResult> RefreshTokenAsync(string email, string refreshToken);
	}
}
