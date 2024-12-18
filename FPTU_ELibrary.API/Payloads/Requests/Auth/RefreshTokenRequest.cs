namespace FPTU_ELibrary.API.Payloads.Requests.Auth;

public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = null!;
    public string AccessToken { get; set; } = null!;
}