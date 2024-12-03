namespace FPTU_ELibrary.API.Payloads.Requests.Auth;

public class CreateUserRequest
{
    public string Email { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
}