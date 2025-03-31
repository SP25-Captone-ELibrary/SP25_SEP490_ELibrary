using FPTU_ELibrary.Application.Dtos.Recommendation;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;

namespace FPTU_ELibrary.Application.Services.IServices;

public interface IRecommenderService
{
    Task<IServiceResult> GetRecommendedItemAsync(string email, RecommendFilterDto filter);
}