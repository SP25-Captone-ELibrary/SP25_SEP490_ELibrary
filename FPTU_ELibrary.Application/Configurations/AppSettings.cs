namespace FPTU_ELibrary.Application.Configurations
{
	//	Summary:
	//		Configure system configuration elements
	public class AppSettings
	{
		public int MaxRefreshTokenLifeSpan { get; set; }
		public string RecoveryPasswordRedirectUri { get; set; } = string.Empty;
		public int PageSize { get; set; }
		public string AESKey { get; set; } = null!;
		public string AESIV { get; set; } = null!;
	}
}
