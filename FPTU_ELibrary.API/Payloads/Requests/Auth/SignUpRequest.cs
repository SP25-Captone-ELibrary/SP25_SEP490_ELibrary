using FPTU_ELibrary.Application.Dtos;

namespace FPTU_ELibrary.API.Payloads.Requests.Auth
{
    public class SignUpRequest
    {
        public string? UserCode { get; set; }
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
    }
}
