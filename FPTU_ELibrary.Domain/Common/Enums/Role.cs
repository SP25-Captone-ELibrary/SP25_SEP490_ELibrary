using System.ComponentModel;

namespace FPTU_ELibrary.Domain.Common.Enums
{
	public enum Role
	{
		//	RoleType: [USER]
        [Description("Siêu quản trị hệ thống")]
        SuperAdministration,
		[Description("Quản trị hệ thống")]
		Administration,
		[Description("Sinh viên")]
		Student,
		[Description("Giảng viên")]
		Teacher,
		[Description("Người dùng thông thường")]
		GeneralMember,
		
		//	RoleType: [EMPLOYEE]
		// Group 1: Professional Library Staff
		[Description("Thủ thư trưởng")]
		HeadLibrarian,

		[Description("Quản lí thư viện")]
		LibraryManager,

		// Group 2: Support Staff
		[Description("Thủ thư")]
		Librarian,

		[Description("Trợ lý thư viện")]
		LibraryAssistant,
	}
}
