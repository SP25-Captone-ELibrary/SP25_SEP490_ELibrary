using FPTU_ELibrary.Application.Dtos.Auth;
using System.ComponentModel.DataAnnotations;

namespace FPTU_ELibrary.API.Payloads.Requests.Auth
{
    public class SignInRequest
    {
        // [Required]
        // [EmailAddress]
        public string Email { get; set; } = string.Empty;

        // [Required]
        // public string Password { get; set; } = string.Empty;
    }
}
