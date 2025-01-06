using FPTU_ELibrary.Application.Dtos.AIServices.Classification;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using Microsoft.AspNetCore.Http;

namespace FPTU_ELibrary.Application.Services.IServices;

public interface IAIClassificationService
{
    // Task<IServiceResult> TrainModel(List<TrainedBookDetailDto> req, string email);
    Task<IServiceResult> TrainModelAfterCreate(Guid bookCode,List<IFormFile> images, string email);
    Task<IServiceResult> TrainModelWithoutCreate(int editionId, List<IFormFile> images, string email);
    Task<IServiceResult> PredictAsync(IFormFile image);
}