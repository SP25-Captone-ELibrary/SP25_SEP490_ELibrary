namespace FPTU_ELibrary.API.Payloads.Requests.Auth;

public class FacebookAuthRequest
{
    public string AccessToken { get; set; } = null!;
    public int ExpiresIn { get; set; }
}