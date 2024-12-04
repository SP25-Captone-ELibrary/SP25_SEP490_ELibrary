namespace FPTU_ELibrary.API.Payloads.Requests.Auth;

public class ChangePasswordRequest
{
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string? Token { get; set; }
}