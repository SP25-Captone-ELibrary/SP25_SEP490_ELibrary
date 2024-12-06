namespace FPTU_ELibrary.API.Payloads.Requests.Auth;

public class SignInWithOtpRequest
{
    public string Email { get; set; } = null!;
    public string Otp { get; set; } = null!;
}