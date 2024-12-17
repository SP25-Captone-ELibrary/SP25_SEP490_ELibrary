namespace FPTU_ELibrary.API.Payloads.Requests.Auth;

public class ValidateMfaRequest
{
    public string Email { get; set; } = null!;
    public string Otp { get; set; } = null!;
}