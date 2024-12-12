using Newtonsoft.Json;

namespace FPTU_ELibrary.Application.Dtos.Roles
{
	public class SystemRoleDto
	{
		// Key
		public int RoleId { get; set; }

		// Role detail
		public string VietnameseName { get; set; } = null!;
		public string EnglishName { get; set; } = null!;
		public string RoleType { get; set; } = null!;
		
		// Role Permissions
		[JsonIgnore]
		public ICollection<RolePermissionDto> RolePermissions { get; set; } = new List<RolePermissionDto>();
	}
}
