using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using Microsoft.AspNetCore.Http;

namespace FPTU_ELibrary.Application.Services.IServices;

public interface IFaceDetectionService
{
    Task<IServiceResult> DetectFaceAsync(IFormFile file, string[] attributes);
}