namespace FPTU_ELibrary.API.Payloads.Requests.Auth;

public class ValidateMfaBackupCodeRequest
{
    public string Email { get; set; } = null!;
    public string BackupCode { get; set; } = null!;
}