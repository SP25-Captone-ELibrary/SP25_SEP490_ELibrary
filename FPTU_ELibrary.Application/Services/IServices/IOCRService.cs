using FPTU_ELibrary.Application.Dtos.AIServices;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using Microsoft.AspNetCore.Http;

namespace FPTU_ELibrary.Application.Services.IServices;

public interface IOCRService
{
    // Task<IServiceResult> CheckBookInformationAsync(CheckedBookEditionDto dto);
    // Task<IServiceResult> CheckTrainingInputAsync(int bookEditionId, List<IFormFile>images);
    Task<IServiceResult> CheckBookInformationAsync(CheckedItemDto dto);
    Task<IServiceResult> OcrDetailAsync(IFormFile image, int bestItemId);
}