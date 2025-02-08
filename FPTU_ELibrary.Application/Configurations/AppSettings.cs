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
		public int InstanceBarcodeNumLength { get; set; } 
		public string AESKey { get; set; } = null!;
		public string AESIV { get; set; } = null!;
	}
}
