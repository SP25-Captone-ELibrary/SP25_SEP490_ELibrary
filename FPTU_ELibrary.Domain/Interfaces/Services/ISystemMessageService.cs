using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications.Interfaces;
using Microsoft.AspNetCore.Http;

namespace FPTU_ELibrary.Domain.Interfaces.Services;

public interface ISystemMessageService
{
    Task<string> GetMessageAsync(string msgId);
    Task<IServiceResult> ImportToExcelAsync(IFormFile file);
    Task<IServiceResult> ExportToExcelAsync();
}