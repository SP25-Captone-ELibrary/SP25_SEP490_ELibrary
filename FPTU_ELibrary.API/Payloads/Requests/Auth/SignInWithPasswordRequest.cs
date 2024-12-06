namespace FPTU_ELibrary.API.Payloads.Requests.Auth;

public class SignInWithPasswordRequest
{
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
}