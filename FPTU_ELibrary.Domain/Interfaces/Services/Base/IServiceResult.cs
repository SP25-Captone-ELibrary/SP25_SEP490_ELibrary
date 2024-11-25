namespace FPTU_ELibrary.Domain.Interfaces.Services.Base
{
	public interface IServiceResult
	{
		int Status { get; set; }
		string? Message { get; set; }
		object? Data { get; set; }
	}
}
