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
		[Description("Bạn đọc")]
		LibraryPatron,
		
		// RoleType: [EMPLOYEE]
		// Group 1: Professional Library Staff
		[Description("Thủ thư trưởng")]
		HeadLibrarian,
		
		// Group 2: Support Staff
		[Description("Thủ thư")]
		Librarian
	}
}
