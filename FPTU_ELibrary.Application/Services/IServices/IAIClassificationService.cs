using FPTU_ELibrary.Application.Dtos.AIServices.Classification;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using Microsoft.AspNetCore.Http;

namespace FPTU_ELibrary.Application.Services.IServices;

public interface IAIClassificationService
{
    // Task<IServiceResult> TrainModel(List<TrainedBookDetailDto> req, string email);
    // Task<IServiceResult> TrainModelAfterCreate(Guid bookCode,List<IFormFile> images, string email);
    // Task<IServiceResult> TrainModelWithoutCreate(int editionId, List<IFormFile> images, string email);
    // Task<IServiceResult> PredictAsync(IFormFile image);
    // Task<IServiceResult> Recommendation(IFormFile image);
    Task<IServiceResult> GetAvailableGroup(string email, int rootItemId, List<int>? otherItemIds);
    Task<IServiceResult> IsAbleToCreateGroup(int rootItemId, List<int>? otherItemIds);
    Task<IServiceResult> TrainModel(Guid trainingCode, List<IFormFile> images, string email);
    Task<IServiceResult> PredictAsync(IFormFile image);
    Task<IServiceResult> PredictWithEmguAsync(IFormFile image);
    Task<IServiceResult> RecommendBook(IFormFile image);
    Task<IServiceResult> RecommendBook(int currentItemId);
    Task<IServiceResult> GetAndGradeAllSuitableItemsForGrouping(int rootItemId);
    Task<IServiceResult> GetAndGradeAllSuitableItemsForGrouping();
    Task<IServiceResult> NumberOfGroupForTraining();
    Task<IServiceResult> ExtendModelTraining(TrainedBookDetailDto dto, string email);
    Task<IServiceResult> ExtendModelTraining(IDictionary<int,List<int>>itemIdsDic, IDictionary<int,List<string>>imagesDic,string email);
    Task<IServiceResult> IsAvailableToTrain();
}