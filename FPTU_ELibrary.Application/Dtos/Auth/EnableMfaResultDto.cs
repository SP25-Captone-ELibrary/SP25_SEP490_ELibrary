namespace FPTU_ELibrary.Application.Dtos.Auth;

public class EnableMfaResultDto
{
    public string QrCodeImage { get; set; } = null!;
    public IEnumerable<string> BackupCodes { get; set; } = null!;
}