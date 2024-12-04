namespace FPTU_ELibrary.Application.Dtos.Auth;

public class RecoveryPasswordResultDto
{
    public string Token { get; set; } = null!;
    public string Email { get; set; } = null!;
}