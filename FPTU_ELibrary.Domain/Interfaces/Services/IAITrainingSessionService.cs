using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;

namespace FPTU_ELibrary.Domain.Interfaces.Services;

public interface IAITrainingSessionService<TDto> : IGenericService<AITrainingSession, TDto, int>
    where TDto : class
{
    Task<IServiceResult> GetByLibraryItemIdAsync(int libraryItemId);
    Task<IServiceResult> UpdateSuccessSessionStatus(int sessionId, bool isSuccess, string? errorMessage = null);
    Task<IServiceResult> UpdatePercentage(int sessionId, int? percentage);
}
