using FPTU_ELibrary.Domain.Interfaces.Services.Base;

namespace FPTU_ELibrary.Domain.Interfaces.Services
{
    public interface IAuthenticationService<TDto>
        where TDto : class
    {
        Task<IServiceResult> SignInAsync(string email);
        Task<IServiceResult> SignInWithPasswordAsync(TDto user);
        Task<IServiceResult> SignInWithOtpAsync(string otp, TDto user);
        Task<IServiceResult> SignInAsEmployeeAsync(TDto user);
        Task<IServiceResult> SignInWithGoogleAsync(string code);
        Task<IServiceResult> SignInWithFacebookAsync(string accessToken, int expiresIn);
        Task<IServiceResult> SignUpAsync(TDto user);
        Task<IServiceResult> ConfirmEmailAsync(string email, string emailVerificationCode);
        Task<IServiceResult> RefreshTokenAsync(string email, string userType, string name,
            string roleName, string tokenId, string refreshToken);

        Task<IServiceResult> VerifyOtpAsync(string email, string otp);
        Task<IServiceResult> ForgotPasswordAsync(string email);
        Task<IServiceResult> ChangePasswordAsync(string email, string password, string? token = null);
        Task<IServiceResult> ChangePasswordAsEmployeeAsync(string email, string password, string? token = null);
    }
}
