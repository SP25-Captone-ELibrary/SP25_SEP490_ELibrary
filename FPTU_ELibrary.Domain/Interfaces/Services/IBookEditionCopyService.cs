using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;

namespace FPTU_ELibrary.Domain.Interfaces.Services;

public interface IBookEditionCopyService<TDto> : IGenericService<BookEditionCopy, TDto, int>
    where TDto : class
{
    Task<IServiceResult> AddRangeToBookEditionAsync(int bookEditionId, List<string> editionCopyCodes);
    Task<IServiceResult> UpdateRangeAsync(List<int> bookEditionCopyIds, string status);
    Task<IServiceResult> SoftDeleteAsync(int bookEditionCopyId);
    Task<IServiceResult> SoftDeleteRangeAsync(List<int> bookEditionCopyIds);
    Task<IServiceResult> UndoDeleteAsync(int bookEditionCopyId);
    Task<IServiceResult> UndoDeleteRangeAsync(List<int> bookEditionCopyIds);
    Task<IServiceResult> DeleteRangeAsync(List<int> bookEditionCopyIds);
}