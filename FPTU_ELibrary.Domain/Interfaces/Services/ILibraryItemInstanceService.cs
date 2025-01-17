using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;

namespace FPTU_ELibrary.Domain.Interfaces.Services;

public interface ILibraryItemInstanceService<TDto> : IGenericService<LibraryItemInstance, TDto, int>
    where TDto : class
{
     // TODO: Fix conflicts
    // Task<IServiceResult> AddRangeToBookEditionAsync(int bookEditionId, List<TDto> bookEditionCopies);
    // Task<IServiceResult> UpdateRangeAsync(int bookEditionId, List<int> bookEditionCopyIds, string status);
    // Task<IServiceResult> SoftDeleteAsync(int bookEditionCopyId);
    // Task<IServiceResult> SoftDeleteRangeAsync(int bookEditionId, List<int> bookEditionCopyIds);
    // Task<IServiceResult> UndoDeleteAsync(int bookEditionCopyId);
    // Task<IServiceResult> UndoDeleteRangeAsync(int bookEditionId, List<int> bookEditionCopyIds);
    // Task<IServiceResult> DeleteRangeAsync(int bookEditionId, List<int> bookEditionCopyIds);
    Task<IServiceResult> CountTotalItemInstanceAsync(int libraryItemId);
    Task<IServiceResult> CountTotalItemInstanceAsync(List<int> libraryItemIds);
}