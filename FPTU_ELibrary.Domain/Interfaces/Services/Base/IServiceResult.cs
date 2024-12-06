namespace FPTU_ELibrary.Domain.Interfaces.Services.Base
{
	public interface IServiceResult<T> 
	{
		string ResultCode { get; set; }
		string? Message { get; set; }
		T? Data { get; set; }
	}
	
	public interface IServiceResult
	{
		string ResultCode { get; set; }
		string? Message { get; set; }
		object? Data { get; set; }
	}
}
