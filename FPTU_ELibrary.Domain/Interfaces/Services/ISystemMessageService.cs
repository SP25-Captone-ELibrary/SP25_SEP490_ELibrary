using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using Microsoft.AspNetCore.Http;

namespace FPTU_ELibrary.Domain.Interfaces.Services;

public interface ISystemMessageService
{
    Task<IServiceResult> ImportToExcelAsync(IFormFile file);
    Task<IServiceResult> ExportToExcelAsync();
    Task<IServiceResult> GetByIdAsync(string msgId);
}