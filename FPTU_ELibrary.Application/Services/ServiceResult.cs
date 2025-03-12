using FPTU_ELibrary.Domain.Interfaces.Services.Base;

namespace FPTU_ELibrary.Application.Services
{
	public class ServiceResult : IServiceResult
    {
        public string ResultCode { get; set; }
        public string? Message { get; set; }
        public object? Data { get; set; }

        public ServiceResult()
        {
            ResultCode = string.Empty;
            Message = string.Empty;
        }
        
        public ServiceResult(string resultCode, string message)
        {
            ResultCode = resultCode;
            Message = message;
        }

        public ServiceResult(string resultCode, string message, object? data)
        {
            ResultCode = resultCode;
            Message = message;
            Data = data;
        }
    }

    public class ServiceResult<T> : IServiceResult<T>
    {
        public string ResultCode { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
        
        public ServiceResult(string resultCode, string message)
        {
            ResultCode = resultCode;
            Message = message;
        }

        public ServiceResult(string resultCode, string message, T? data)
        {
            ResultCode = resultCode;
            Message = message;
            Data = data;
        }
    }
}
