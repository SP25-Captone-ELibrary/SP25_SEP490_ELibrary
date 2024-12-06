namespace FPTU_ELibrary.Application.Configurations
{
	public class WebTokenSettings
	{
        public bool ValidateIssuerSigningKey { get; set; }
        public string IssuerSigningKey { get; set; } = string.Empty;
        public bool ValidateIssuer { get; set; }
        public string ValidIssuer { get; set; } = string.Empty;
        public bool ValidateAudience { get; set; }
        public string ValidAudience { get; set; } = string.Empty;
        public bool RequireExpirationTime { get; set; }
        public bool ValidateLifetime { get; set; }
        public int TokenLifeTimeInMinutes { get; set; }
        public int RefreshTokenLifeTimeInMinutes { get; set; }
        public int RecoveryPasswordLifeTimeInMinutes { get; set; }
    }
}
