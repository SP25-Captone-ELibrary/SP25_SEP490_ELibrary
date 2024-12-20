using FPTU_ELibrary.Domain.Interfaces.Services.Base;

namespace FPTU_ELibrary.Domain.Interfaces.Services
{
    public interface IAuthenticationService<TDto>
        where TDto : class
    {
        Task<IServiceResult> UpdateProfileAsync(TDto dto);
        Task<IServiceResult> GetCurrentUserAsync(string email);
        Task<IServiceResult> GetMfaBackupAsync(string email);
        Task<IServiceResult> SignInAsync(string email);
        Task<IServiceResult> SignInWithPasswordAsync(TDto user);
        Task<IServiceResult> SignInWithOtpAsync(string otp, TDto user);
        Task<IServiceResult> SignInAsAdminAsync(TDto user);
        Task<IServiceResult> SignInAsEmployeeAsync(TDto user);
        Task<IServiceResult> SignInWithGoogleAsync(string code);
        Task<IServiceResult> SignInWithFacebookAsync(string accessToken, int expiresIn);
        Task<IServiceResult> SignUpAsync(TDto user);
        Task<IServiceResult> ConfirmEmailForSignUpAsync(string email, string emailVerificationCode);
        Task<IServiceResult> RefreshTokenAsync(string accessToken, string refreshTokenId);
        Task<IServiceResult> VerifyChangePasswordOtpAsync(string email, string otp);
        Task<IServiceResult> ResendOtpAsync(string email);
        Task<IServiceResult> ForgotPasswordAsync(string email);
        Task<IServiceResult> ChangePasswordAsync(string email, string password, string? token = null);
        Task<IServiceResult> ChangePasswordAsEmployeeAsync(string email, string password, string? token = null);
        Task<IServiceResult> EnableMfaAsync(string email);
        Task<IServiceResult> ValidateMfaAsync(string email, string otp);
        Task<IServiceResult> ValidateMfaBackupCodeAsync(string email, string backupCode);
        Task<IServiceResult> RegenerateMfaBackupCodeAsync(string email);
        Task<IServiceResult> ConfirmRegenerateMfaBackupCodeAsync(string email, string otp, string token);
    }
}
