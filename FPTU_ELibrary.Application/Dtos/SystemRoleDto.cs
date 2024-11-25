namespace FPTU_ELibrary.Application.Dtos
{
	public class SystemRoleDto
	{
		// Key
		public int RoleId { get; set; }

		// Role detail
		public string VietnameseName { get; set; } = null!;
		public string EnglishName { get; set; } = null!;
	}
}
