using FPTU_ELibrary.Application.Dtos.AIServices.Detection;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using Microsoft.AspNetCore.Http;

namespace FPTU_ELibrary.Application.Services.IServices;

public interface IAIDetectionService
{
    Task<List<BoxDto>> DetectAsync(IFormFile image);

    Task<IServiceResult> ValidateImportTraining(int itemId, List<IFormFile> compareList);
    Dictionary<string, int> CountObjectsInImage(List<DetectResultDto> detectedResults, BoxDto? bookBox);
    Task<List<DetectResultDto>> DetectAllAsync(IFormFile image);
    Task<IServiceResult> RawDetectAsync(IFormFile image);
}