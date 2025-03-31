using FPTU_ELibrary.Domain.Interfaces.Services.Base;

namespace FPTU_ELibrary.Application.Services.IServices;

/// <summary>
/// Provide abstract methods to effective manage memory caching within the system
/// This class only use by other services, not allow to access from presenter layer
/// </summary>
public interface ICacheService
{
    Task<IServiceResult> GetOrAddLibraryItemForRecommendationAsync();
}