namespace FPTU_ELibrary.API.Payloads.Requests.Auth;

public class OtpVerificationRequest
{
    public string Email { get; set; } = null!;
    public string Otp { get; set; } = null!;
}