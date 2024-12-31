using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using Microsoft.AspNetCore.Http;

namespace FPTU_ELibrary.Application.Services.IServices;

public interface IAIClassificationService
{
    Task<IServiceResult> TrainModel(int bookId, List<IFormFile> imageList,string email);
}