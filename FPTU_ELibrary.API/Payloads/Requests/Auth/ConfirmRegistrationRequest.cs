namespace FPTU_ELibrary.API.Payloads.Requests.Auth
{
	public class ConfirmRegistrationRequest
	{
		public string Email { get; set; } = null!;
		public string EmailVerificationCode { get; set; } = null!;
    }
}
