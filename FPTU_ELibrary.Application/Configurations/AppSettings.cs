namespace FPTU_ELibrary.Application.Configurations
{
	//	Summary:
	//		Configure system configuration elements
	public class AppSettings
	{
		public int PageSize { get; set; }
		public string LibraryLocation { get; set; } = null!;
		public string LibraryContact { get; set; } = null!;
		public string LibraryName { get; set; } = null!;
		public string LibraryCardBarcodePrefix { get; set; } = null!;
		public int InstanceBarcodeNumLength { get; set; } 
		public string AESKey { get; set; } = null!;
		public string AESIV { get; set; } = null!;
		public LibrarySchedule LibrarySchedule { get; set; } = null!;
	}

	public class LibrarySchedule
	{
		public List<DaySchedule> Schedules { get; set; } = new();
	}

	public class DaySchedule
	{
		// ["Monday", "Tuesday", ...]
		public List<DayOfWeek> Days { get; set; } = new();
		// "07:30:00" -> TimeSpan.FromHours(7.5)
		public TimeSpan Open { get; set; }
		public TimeSpan Close { get; set; }
	}
}
