namespace FPTU_ELibrary.Application.Dtos.Auth
{
	public class AuthenticateResultDto
	{
		public string AccessToken { get; set; } = null!;
		public string RefreshToken { get; set; } = null!;
		public DateTime ValidTo { get; set; }
    }
}
