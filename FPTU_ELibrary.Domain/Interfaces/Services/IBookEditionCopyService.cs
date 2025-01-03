using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;

namespace FPTU_ELibrary.Domain.Interfaces.Services;

public interface IBookEditionCopyService<TDto> : IGenericService<BookEditionCopy, TDto, int>
    where TDto : class
{
    Task<IServiceResult> AddRangeToBookEditionAsync(int bookEditionId, List<TDto> bookEditionCopies);
    Task<IServiceResult> UpdateRangeAsync(int bookEditionId, List<int> bookEditionCopyIds, string status);
    Task<IServiceResult> SoftDeleteAsync(int bookEditionCopyId);
    Task<IServiceResult> SoftDeleteRangeAsync(int bookEditionId, List<int> bookEditionCopyIds);
    Task<IServiceResult> UndoDeleteAsync(int bookEditionCopyId);
    Task<IServiceResult> UndoDeleteRangeAsync(int bookEditionId, List<int> bookEditionCopyIds);
    Task<IServiceResult> DeleteRangeAsync(int bookEditionId, List<int> bookEditionCopyIds);
    Task<IServiceResult> CountTotalEditionCopyAsync(int bookEditionId);
    Task<IServiceResult> CountTotalEditionCopyAsync(List<int> bookEditionIds);
}