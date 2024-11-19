using System.ComponentModel;

namespace FPTU_ELibrary.Domain.Common.Enums
{
	public enum Role
	{
		[Description("Quản trị hệ thống")]
		Administration,
		[Description("Sinh viên")]
		Student,
		[Description("Giảng viên")]
		Teacher,
		[Description("Người dùng thông thường")]
		GeneralMember
	}
}
