using FPTU_ELibrary.Application.Dtos.AIServices.Detection;
using Microsoft.AspNetCore.Http;

namespace FPTU_ELibrary.Application.Services.IServices;

public interface IAIDetectionService
{
    Task<List<BoxDto>> DetectAsync(IFormFile image);
}