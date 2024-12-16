namespace FPTU_ELibrary.API.Payloads.Requests.Auth;

public class RegenerateBackupConfirmRequest
{
    public string Otp { get; set; } = null!;
    public string Token { get; set; } = null!;
}